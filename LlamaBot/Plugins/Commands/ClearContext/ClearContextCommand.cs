using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.ClearContext
{
    internal class ClearContextCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("Optionally clears the underlying KV Cache")]
        public bool IncludeCache { get; set; }
    }
}