using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.Interrupt
{
    internal class InterruptCommandProvider : ICommandProvider<InterruptCommand>
    {
        public string Command => "interrupt";

        public string Description => "Interrupts the bots current message generation";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(InterruptCommand command)
        {
            _chatContext.TryInterrupt();

            await command.Command.DeleteOriginalResponseAsync();

            return CommandResult.Success();
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}