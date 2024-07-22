using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Interfaces
{
    public interface ICommandProvider : IPlugin
    {
        string Command { get; }

        string Description { get; }

        SlashCommandOption[] SlashCommandOptions { get; }
    }

    public interface ICommandProvider<in TCommand> : ICommandProvider
    {
        Task<CommandResult> OnCommand(TCommand command);
    }
}