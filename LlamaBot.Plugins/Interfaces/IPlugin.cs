using LlamaBot.Plugins.EventArgs;

using LlamaBot.Plugins.EventResults;

namespace LlamaBot.Plugins.Interfaces
{
    public interface IPlugin
    {
        Task<InitializationResult> OnInitialize(InitializationEventArgs args);
    }
}