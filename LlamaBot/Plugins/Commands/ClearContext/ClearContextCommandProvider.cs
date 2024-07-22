using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using Loxifi;

namespace LlamaBot.Plugins.Commands.ClearContext
{
    internal class ClearContextCommandProvider : ICommandProvider<ClearContextCommand>
    {
        public string Command => "clear";

        public string Description => "Clears the bots memory";

        public SlashCommandOption[] SlashCommandOptions => [];

        public Task<CommandResult> OnCommand(ClearContextCommand command)
        {
            ulong channelId = command.Channel.Id;

            DateTime triggered = command.Command.CreatedAt.DateTime;

            _metaData.ClearValues[channelId] = triggered;

            StaticConfiguration.Save(_metaData);

            if (includeCache)
            {
                _chatContext.Clear(true);
            }

            return CommandResult.SuccessAsync("Memory Cleared");
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}