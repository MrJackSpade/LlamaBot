using LlamaBot.Plugins.EventArgs;
using LlamaBot.Shared.Models;
using System.Reflection;

namespace LlamaBot.Plugins.Interfaces
{
    public interface IPluginService
    {
        IReadOnlyList<ICommandProvider> CommandProviders { get; }

        Task Command<T>(T command) where T : BaseCommand;

        Task LoadPlugins(Assembly assembly, string assemblyName);

        Task LoadPlugins();

        Task React(ReactionEventArgs args);
    }
}