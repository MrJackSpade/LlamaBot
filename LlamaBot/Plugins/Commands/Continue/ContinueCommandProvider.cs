using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.Continue
{
    internal class ContinueCommandProvider : ICommandProvider<ContinueCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "continue";

        public string Description => "Continues the last response";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(ContinueCommand command)
        {
            if (command.Channel is ISocketMessageChannel smc)
            {
                _llamaBotClient.TryProcessMessageThread(smc);
                await command.Command.DeleteOriginalResponseAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Error($"Requested channel is not {nameof(ISocketMessageChannel)}");
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _pluginService = args.PluginService;
            _discordClient = args.DiscordService;
            _llamaBotClient = args.LlamaBotClient;
            return InitializationResult.SuccessAsync();
        }
    }
}