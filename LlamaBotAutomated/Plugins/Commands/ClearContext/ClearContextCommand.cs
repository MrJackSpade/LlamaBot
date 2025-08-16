using Discord.WebSocket;
using LlamaBotAutomated.Plugins.Commands;
using System.ComponentModel;

namespace LlamaBotAutomated.Plugins.Commands.ClearContext
{
    internal class ClearContextCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("Optionally clears the underlying KV Cache")]
        public bool IncludeCache { get; set; }
    }
}