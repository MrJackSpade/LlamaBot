using LlamaBot.Plugins.EventArgs;

namespace LlamaBot.Plugins.Interfaces
{
    public interface IReactionHandler : IPlugin
    {
        string[] HandledReactions { get; }

        Task OnReaction(ReactionEventArgs args);
    }
}