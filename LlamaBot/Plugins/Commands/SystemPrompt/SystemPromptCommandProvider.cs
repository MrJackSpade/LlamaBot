using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.SystemPrompt
{
    internal class SystemPromptCommandProvider : ICommandProvider<SystemPromptCommand>
    {
        public string Command => "prompt";

        public string Description => "Updates or displays the bots system prompt";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(SystemPromptCommand command)
        {
            if (command.ClearContext)
            {
                await OnClearCommand(command, true);
            }

            if (command.Prompt is null)
            {
                return CommandResult.Success(_systemPrompt);
            }
            else
            {
                _systemPrompt = command.Prompt;
                return CommandResult.Success("System Prompt Updated: " + command.Prompt);
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}