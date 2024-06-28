using Discord;
using Discord.WebSocket;
using LlamaBot.Discord;
using LlamaBot.Discord.Commands;
using LlamaBot.Discord.Extensions;
using LlamaBot.Discord.Model;
using LlamaBot.Extensions;
using LlamaNative.Apis;
using LlamaNative.Chat;
using LlamaNative.Chat.Extensions;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers.Repetition;
using LlamaNative.Sampling.Settings;
using LlamaNative.Utils;
using Loxifi;

namespace LlamaBot
{
    internal class Program
    {
        private static readonly Configuration _configuration = StaticConfiguration.Load<Configuration>();

        private static readonly RecursiveConfigurationReader<Character> _recursiveConfigurationReader = new("Characters");

        private static readonly DateTime _startTime = DateTime.Now;

        private static readonly MetaData _metaData = StaticConfiguration.Load<MetaData>();

        private static IChatContext? _chatContext;

        private static DiscordClient? _discordClient;

        private static RecursiveConfiguration<Character>? _recursiveConfiguration;

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
            if (message.Author.Username == _discordClient.CurrentUser.Username)
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

            await ProcessMessage(message);
        }

        public static async Task ProcessMessage(SocketMessage message)
        {
            Console.Clear();

            _chatContext.Clear();

            InsertContextHeaders();

            int messageStart = _chatContext.MessageCount;

            _chatContext.Insert(messageStart, GetDisplayName(message.Author), message.Content, message.Id);

            DateTime stop = DateTime.MinValue;

            if(_metaData.ClearValues.TryGetValue(message.Channel.Id, out DateTime savedStop))
            {
                stop = savedStop;
            }

            foreach (IMessage? historicalMessage in await message.Channel.GetMessagesAsync(message, Direction.Before, 100).FlattenAsync())
            {
                if(historicalMessage.CreatedAt.DateTime < stop)
                {
                    break;
                }

                if(historicalMessage.Type == MessageType.ApplicationCommand)
                {
                    continue;
                }
                
                _chatContext.Insert(messageStart, GetDisplayName(historicalMessage.Author), historicalMessage.Content, historicalMessage.Id);

                if (_chatContext.AvailableBuffer < 1000)
                {
                    break;
                }
            }

            using IDisposable typingState = message.Channel.EnterTypingState();

            foreach (ChatMessage cm in _chatContext.ReadResponse())
            {
                await message.Channel.SendMessageAsync(cm.Content);
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

            Console.WriteLine("Connecting to Discord...");

            await _discordClient.Connect();

            await _discordClient.SetUserName(_recursiveConfiguration.Configuration.ChatSettings.BotName);

            await _discordClient.AddCommand<ClearCommand>("clear", "clears the bots memory", OnClearCommand);

            Console.WriteLine("Connected.");

            _discordClient.MessageReceived += MessageReceived;
        }

        static async Task<CommandResult> OnClearCommand(ClearCommand clearCommand)
        {
            ulong channelId = clearCommand.Channel.Id;

            DateTime triggered = clearCommand.Command.CreatedAt.DateTime;

            _metaData.ClearValues[channelId] = triggered;

            StaticConfiguration.Save(_metaData);

            return CommandResult.Success("Memory Cleared");
        }

        private static void InsertContextHeaders()
        {
            if (_recursiveConfiguration.Resources.TryGetValue("System.txt", out string? systemText))
            {
                _chatContext.SendMessage("System", systemText);
            }

            foreach (ChatMessage cm in Character.ChatMessages)
            {
                _chatContext.SendMessage(cm);
            }
        }

        private static async Task Main(string[] args)
        {
            _recursiveConfiguration = _recursiveConfigurationReader.Read(args[0]);

            InitializeContext();

            await InitializeDiscordClient();

            await Task.Delay(-1);
        }
    }
}