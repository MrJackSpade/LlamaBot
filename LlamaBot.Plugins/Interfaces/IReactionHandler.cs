using LlamaBot.Models.Events;
using LlamaBot.Plugins.Interfaces;

namespace LlamaBot.Plugins.Interfaces
{
    public interface IReactionHandler : IPlugin
    {
        string[] HandledReactions { get; }

        Task OnReaction(ReactionEventArgs args);
    }
}