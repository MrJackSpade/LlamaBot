using Discord;
using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;
using LlamaNative.Utils;

namespace LlamaBot.Plugins.Commands.Generate
{
    internal class GenerateCommandProvider : ICommandProvider<GenerateCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "Generate";

        public string Description => "Generates a message with a specific username";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(GenerateCommand command)
        {
            Ensure.NotNull(_llamaBotClient);

            if (command.Channel is ISocketMessageChannel smc)
            {
                string? userName = command.UserName;

                // If no username provided, use the author of the last bot message
                if (string.IsNullOrWhiteSpace(userName))
                {
                    IMessage? lastBotMessage = await _llamaBotClient.TryGetLastBotMessage(smc);
                    if (lastBotMessage is not null)
                    {
                        ParsedMessage parsed = _llamaBotClient.ParseMessage(lastBotMessage);
                        userName = parsed.Author;
                    }
                }

                await command.Command.DeleteOriginalResponseAsync();

                _llamaBotClient.TryProcessMessageAsync(smc, new ReadResponseSettings()
                {
                    RespondingUser = userName
                }, CancellationToken.None);

                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Error($"Requested channel is not {nameof(ISocketMessageChannel)}");
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;
            _pluginService = args.PluginService;
            _discordClient = args.DiscordService;
            return InitializationResult.SuccessAsync();
        }
    }
}