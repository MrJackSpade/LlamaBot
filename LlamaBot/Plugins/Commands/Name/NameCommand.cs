using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.Name
{
    internal class NameCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("The new name to use for yourself")]
        public string Username { get; set; }
    }
}
