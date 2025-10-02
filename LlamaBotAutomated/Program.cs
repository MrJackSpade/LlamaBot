using Discord;
using Discord.WebSocket;
using LlamaBot;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Plugins.Services;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Loggers;
using LlamaBot.Shared.Models;
using LlamaBotAutomated.Discord;
using LlamaNative.Chat.Models;
using LlamaNative.Utils;
using Loxifi;
using System.Reflection;

namespace LlamaBotAutomated
{
    internal class Program
    {
        private static readonly Configuration _configuration;

        private static readonly DiscordService _discordClient;

        private static readonly ILogger _logger = new ConsoleLogger();

        private static readonly RecursiveConfigurationReader<Character> _recursiveConfigurationReader = new("Characters");

        private static readonly DateTime _startTime = DateTime.Now;

        private static LlamaBotClient _llamaBotClient;

        private static IPluginService? _pluginService;

        private static RecursiveConfiguration<Character>? _recursiveConfiguration;

        private static readonly Thread MessageThread;

        static Program()
        {
            _configuration = StaticConfiguration.Load<Configuration>();
            _discordClient = new(_configuration.DiscordToken);
            MessageThread = new Thread(async () => await MessageLoop());
        }

        public static CancellationTokenSource _cancellationTokenSource = new();

        private static async Task MessageLoop()
        {
            ISocketMessageChannel channel = (ISocketMessageChannel)_discordClient.GetChannel(_configuration.ChannelIds.Single());

            string lastUser = string.Empty;

            var chatSettings = _recursiveConfiguration.Configuration.ChatSettings;
            do
            {
                try
                {
                    string thisUser = chatSettings.BotName;

                    if (chatSettings.AlternateNames.Length > 0)
                    {
                        List<string> allNames =
                        [
                            chatSettings.BotName, .. chatSettings.AlternateNames
                        ];

                        List<string> namePool = [.. allNames.Where(n => n != lastUser)];

                        thisUser = namePool[new Random().Next(0, namePool.Count)];
                    }

                    var readResponseSettings = new ReadResponseSettings()
                    {
                        RespondingUser = thisUser,
                        PrependDefaultUser = chatSettings.AlternateNames.Length > 0
                    };

                    _selfMessageRecieved.Reset();

                    await _llamaBotClient.ProcessMessage(channel, readResponseSettings, _cancellationTokenSource.Token);

                    _selfMessageRecieved.Wait();

                    lastUser = thisUser;
                }
                catch(OperationCanceledException)
                {

                }
                finally
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }
            } while (true);
        }

        private static async Task InitializeDiscordClient()
        {
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
        }

        private static bool IsValidSource(IChannel channel)
        {
            if (channel is SocketThreadChannel socketThreadChannel)
            {
                ulong parentChannel = socketThreadChannel.ParentChannel.Id;

                if (_configuration.ChannelIds is not null)
                {
                    if (_configuration.ChannelIds.Contains(parentChannel))
                    {
                        return true;
                    }
                }
            }

            if (channel is SocketTextChannel socketTextChannel)
            {
                if (_configuration.ChannelIds is not null)
                {
                    if (_configuration.ChannelIds.Contains(socketTextChannel.Id))
                    {
                        return true;
                    }
                }
            }

            if (channel is SocketDMChannel socketDMChannel)
            {
                if (_configuration.UserIds is not null)
                {
                    if (_configuration.UserIds.Contains(socketDMChannel.Users.ToArray()[1].Id))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static async Task Main(string[] args)
        {
            _recursiveConfiguration = _recursiveConfigurationReader.Read(args[0]);

            _recursiveConfiguration.Resources.TryGetValue("System.txt", out string? systemPrompt);
            _recursiveConfiguration.Resources.TryGetValue("Think.txt", out string? thinkPrompt);

            await InitializeDiscordClient();

            _llamaBotClient = new LlamaBotClient(
                _recursiveConfiguration.Configuration,
                new ChannelSettings(systemPrompt, thinkPrompt),
                _discordClient.CurrentUser.Id);

            _pluginService = new PluginService(_logger, _discordClient, _llamaBotClient);

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
                                                     if (!IsValidSource(c.Channel))
                                                     {
                                                         return Task.FromResult(CommandResult.Error("Bot not registered in this channel"));
                                                     }

                                                     object result = invocationMethod.Invoke(commandProvider, [c])!;

                                                     _cancellationTokenSource.Cancel();

                                                     return (Task<CommandResult>)result;
                                                 },
                                                 commandProvider.SlashCommandOptions);
            }

            _discordClient.ReactionAdded += _pluginService.React;
            _discordClient.MessageReceived += MessageReceived;
            MessageThread.Start();

            await Task.Delay(-1);
        }

        private static readonly ManualResetEventSlim _selfMessageRecieved = new(false);

        private static async Task MessageReceived(SocketMessage message)
        {
            if(message.Author.Username == _llamaBotClient.BotName)
            {
                _selfMessageRecieved.Set();
                return;
            }

            if (!IsValidSource(message.Channel))
            {
                return;
            }

            _cancellationTokenSource.Cancel();
        }
    }
}