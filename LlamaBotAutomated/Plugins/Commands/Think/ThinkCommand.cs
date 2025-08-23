using Discord.WebSocket;
using LlamaBotAutomated.Plugins.Commands;
using System.ComponentModel;

namespace LlamaBotAutomated.Plugins.Commands.Think
{
    internal class ThinkCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("A forced thought value")]
        public string Think { get; set; }

        [Description("The user name for which this thought will apply")]
        public string UserName { get; set; }
    }
}