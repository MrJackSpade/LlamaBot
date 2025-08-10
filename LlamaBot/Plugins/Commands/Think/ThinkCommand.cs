using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.Think
{
    internal class ThinkCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("A forced thought value")]
        public string Think { get; set; }
    }
}