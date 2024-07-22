using Discord;
using Discord.WebSocket;
using LlamaBot.Discord;
using LlamaBot.Extensions;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Services;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Loggers;
using LlamaBot.Shared.Models;
using LlamaNative.Chat;
using LlamaNative.Chat.Extensions;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers.Repetition;
using LlamaNative.Sampling.Settings;
using LlamaNative.Utils;
using Loxifi;
using System.Reflection;

namespace LlamaBot
{
    internal class Program
    {
        private static readonly Configuration _configuration = StaticConfiguration.Load<Configuration>();

        private static readonly MetaData _metaData = StaticConfiguration.Load<MetaData>();

        private static readonly RecursiveConfigurationReader<Character> _recursiveConfigurationReader = new("Characters");

        private static readonly DateTime _startTime = DateTime.Now;

        private static IChatContext? _chatContext;

        private static DiscordClient? _discordClient;

        private static IPluginService _pluginService;

        private static readonly ILogger _logger = new ConsoleLogger();

        private static Thread _processMessageThread;

        private static RecursiveConfiguration<Character>? _recursiveConfiguration;

        private static string _systemPrompt = string.Empty;

        private static Character? Character => _recursiveConfiguration?.Configuration;

        private static ChatSettings ChatSettings => Character.ChatSettings;

        public static string GetDisplayName(IUser user)
        {
            if (user.Id == _discordClient.CurrentUser.Id)
            {
                return ChatSettings.BotName;
            }

            if (Character.NameOverride.TryGetValue(user.Username, out string? name))
            {
                return name;
            }

            if (user is IGuildUser guildUser)
            {
                return guildUser.DisplayName;
            }

            return user.Username;
        }

        public static async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.Id == _discordClient.CurrentUser.Id)
            {
                return;
            }

            if (message.Channel is SocketTextChannel socketTextChannel)
            {
                if (!_configuration.ChannelIds.Contains(socketTextChannel.Id))
                {
                    return;
                }
            }
            else if (message.Channel is SocketDMChannel socketDMChannel)
            {
                if (!_configuration.ChannelIds.Contains(socketDMChannel.Users.ToArray()[1].Id))
                {
                    return;
                }
            }
            else
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return;
            }

            TryProcessMessageThread(message.Channel);
        }

        public static async Task ProcessMessage(ISocketMessageChannel channel)
        {
            Console.Clear();

            _chatContext.Clear(false);

            InsertContextHeaders();

            int messageStart = _chatContext.MessageCount;

            DateTime stop = DateTime.MinValue;

            if (_metaData.ClearValues.TryGetValue(channel.Id, out DateTime savedStop))
            {
                stop = savedStop;
            }

            await foreach (IReadOnlyCollection<IMessage>? historicalMessages in channel.GetMessagesAsync(1000))
            {
                bool done = false;

                foreach (IMessage historicalMessage in historicalMessages)
                {
                    if (historicalMessage.CreatedAt.DateTime < stop)
                    {
                        done = true;
                        break;
                    }

                    if (historicalMessage.Type == MessageType.ApplicationCommand)
                    {
                        continue;
                    }

                    _chatContext.Insert(messageStart, GetDisplayName(historicalMessage.Author), historicalMessage.Content, historicalMessage.Id);

                    if (_chatContext.AvailableBuffer < 1000)
                    {
                        done = true;
                        break;
                    }
                }

                if (done)
                {
                    break;
                }
            }

            using IDisposable typingState = channel.EnterTypingState();

            foreach (ChatMessage cm in _chatContext.ReadResponse())
            {
                if (string.IsNullOrEmpty(cm.Content))
                {
                    await channel.SendMessageAsync("[Empty Message]");
                }
                else
                {
                    string content = cm.Content;
                    while (content.Length > 0)
                    {
                        int chunkSize = Math.Min(1950, content.Length);
                        string chunk = content[..chunkSize];
                        await channel.SendMessageAsync(chunk);
                        content = content[chunkSize..];
                    }
                }
            }
        }

        public static void TryProcessMessageThread(ISocketMessageChannel smc)
        {
            if (_processMessageThread is null || _processMessageThread.ThreadState != ThreadState.Running)
            {
                _processMessageThread = new Thread(async () => await ProcessMessage(smc));
                _processMessageThread.Start();
            }
        }

        private static void InitializeContext()
        {
            if (ChatSettings.ResponseStartBlock > 0)
            {
                ChatSettings.SimpleSamplers.Add(
                    new SamplerSetting(
                        nameof(SubsequenceBlockingSampler),
                        new SubsequenceBlockingSamplerSettings()
                        {
                            ResponseStartBlock = ChatSettings.ResponseStartBlock,
                            SubSequence = ChatSettings.ChatTemplate.ToHeader(ChatSettings.BotName)
                        }
                    ));
            }

            _chatContext = LlamaChatClient.LoadChatContext(Character.ChatSettings);
        }

        private static async Task InitializeDiscordClient()
        {
            _discordClient = new(_configuration.DiscordToken);

            Console.WriteLine($"Connecting to Discord with token [{_configuration.DiscordToken}]...");

            await _discordClient.Connect();

            try
            {
                await _discordClient.SetUserName(_recursiveConfiguration.Configuration.ChatSettings.BotName);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to set username: {e.Message}");
            }

            Console.WriteLine("Connected.");

            _discordClient.MessageReceived += MessageReceived;
        }

        private static void InsertContextHeaders()
        {
            if (!string.IsNullOrWhiteSpace(_systemPrompt))
            {
                _chatContext.SendMessage(ChatSettings.SystemPromptUser, _systemPrompt);
            }

            foreach (ChatMessage cm in Character.ChatMessages)
            {
                _chatContext.SendMessage(cm);
            }
        }

        private static async Task Main(string[] args)
        {
            _recursiveConfiguration = _recursiveConfigurationReader.Read(args[0]);

            if (_recursiveConfiguration.Resources.TryGetValue("System.txt", out string? systemText) && !string.IsNullOrWhiteSpace(systemText))
            {
                _systemPrompt = systemText;
            }

            InitializeContext();

            await InitializeDiscordClient();

            _pluginService = new PluginService(_logger, _discordClient);

            await _pluginService.LoadPlugins();

            foreach (ICommandProvider commandProvider in _pluginService.CommandProviders)
            {
                Type parameterType = commandProvider.GetType()
                                                    .GetInterface(typeof(ICommandProvider<>).Name)!
                                                    .GetGenericArguments()[0];

                MethodInfo invocationMethod = commandProvider.GetType().GetMethod(nameof(ICommandProvider<object>.OnCommand))!;

                await _discordClient.AddCommand(commandProvider.Command,
                                                 commandProvider.Description,
                                                 parameterType,
                                                 c =>
                                                 {
                                                     object result = invocationMethod.Invoke(commandProvider, [c])!;
                                                     return (Task<CommandResult>)result;
                                                 },
                                                 commandProvider.SlashCommandOptions);
            }

            _discordClient.ReactionAdded += _pluginService.React;

            await Task.Delay(-1);
        }
    }
}