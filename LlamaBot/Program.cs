using Discord.WebSocket;
using LlamaBot.Discord;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Plugins.Services;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Loggers;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;
using LlamaNative.Utils;
using Loxifi;
using System.Reflection;

namespace LlamaBot
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

        static Program()
        {
            _configuration = StaticConfiguration.Load<Configuration>();
            _discordClient = new(_configuration.DiscordToken);
        }

        public static async Task MessageReceived(SocketMessage message)
        {
            ReadResponseSettings? readResponseSettings = null;

            if (message.Author.Id == _discordClient.CurrentUser.Id)
            {
                return;
            }

            if (_llamaBotClient is null)
            {
                return;
            }

            AutoRespond autoRespond = _llamaBotClient.GetAutoRespond(message.Channel.Id);

            if (autoRespond.Disabled)
            {
                return;
            }
            else if (!string.IsNullOrWhiteSpace(autoRespond.UserName))
            {
                readResponseSettings = new ReadResponseSettings()
                {
                    RespondingUser = autoRespond.UserName
                };

            }

            if (message.Channel is SocketTextChannel socketTextChannel)
            {
                if (_configuration.ChannelIds is not null)
                {
                    if (!_configuration.ChannelIds.Contains(socketTextChannel.Id))
                    {
                        return;
                    }
                }
            }
            else if (message.Channel is SocketDMChannel socketDMChannel)
            {
                if (_configuration.UserIds is not null)
                {
                    if (!_configuration.UserIds.Contains(socketDMChannel.Users.ToArray()[1].Id))
                    {
                        return;
                    }
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

            _llamaBotClient.TryProcessMessageAsync(message.Channel, readResponseSettings);
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

            _discordClient.MessageReceived += MessageReceived;
        }

        private static async Task Main(string[] args)
        {
            _recursiveConfiguration = _recursiveConfigurationReader.Read(args[0]);

            _recursiveConfiguration.Resources.TryGetValue("System.txt", out string systemPrompt);

            await InitializeDiscordClient();

            _llamaBotClient = new LlamaBotClient(_recursiveConfiguration.Configuration, systemPrompt, _discordClient.CurrentUser.Id);

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