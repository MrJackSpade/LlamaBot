using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.ClearContext
{
    internal class ClearContextCommandProvider : ICommandProvider<ClearContextCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "clear";

        public string Description => "Clears the bots memory";

        public SlashCommandOption[] SlashCommandOptions => [];

        public Task<CommandResult> OnCommand(ClearContextCommand command)
        {
            ulong channelId = command.Channel.Id;

            DateTime triggered = command.Command.CreatedAt.DateTime;

            _llamaBotClient.SetClearDate(channelId, triggered);

            if (command.IncludeCache)
            {
                _llamaBotClient.Clear(true);
            }

            return CommandResult.SuccessAsync("Memory Cleared");
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