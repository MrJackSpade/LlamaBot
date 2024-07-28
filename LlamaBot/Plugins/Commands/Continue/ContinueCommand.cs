using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.Continue
{
    internal class ContinueCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("Creates a new message instead of continuing the last one")]
        public bool NewMessage { get; set; }
    }
}