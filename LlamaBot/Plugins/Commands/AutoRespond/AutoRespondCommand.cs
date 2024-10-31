using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.AutoRespond
{
    internal class AutoRespondCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("Disables autorespond")]
        public bool Disabled { get; set; }

        [Description("The user name that should respond")]
        public string UserName { get; set; }
    }
}