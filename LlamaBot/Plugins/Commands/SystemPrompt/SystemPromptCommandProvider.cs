using LlamaBot.Plugins.Commands.ClearContext;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.SystemPrompt
{
    internal class SystemPromptCommandProvider : ICommandProvider<SystemPromptCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _lamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "prompt";

        public string Description => "Updates or displays the bots system prompt";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(SystemPromptCommand command)
        {
            if (command.ClearContext)
            {
                await _pluginService.Command(new ClearContextCommand(command.Command)
                {
                    IncludeCache = true,
                });
            }

            if (command.Prompt is null)
            {
                return CommandResult.Success(_lamaBotClient.SystemPrompt);
            }
            else
            {
                _lamaBotClient.SystemPrompt = command.Prompt;
                return CommandResult.Success("System Prompt Updated: " + command.Prompt);
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _pluginService = args.PluginService;
            _discordClient = args.DiscordService;
            _lamaBotClient = args.LlamaBotClient;
            return InitializationResult.SuccessAsync();
        }
    }
}