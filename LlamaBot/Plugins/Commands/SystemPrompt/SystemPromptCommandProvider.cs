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

            string responseString;

            if (command.Prompt is null)
            {
                responseString = _lamaBotClient.SystemPrompt;
            }
            else
            {
                _lamaBotClient.SystemPrompt = command.Prompt;
                responseString = "System Prompt Updated: " + command.Prompt;
            }

            if (responseString.Length > 1995)
            {
                responseString = responseString[..1990] + "...";
            }

            return CommandResult.Success(responseString);
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