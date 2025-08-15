using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.AutoRespond
{
    internal class AutoRespondCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("The user name that should respond, _ if disabled")]
        public string? UserName { get; set; }
    }
}