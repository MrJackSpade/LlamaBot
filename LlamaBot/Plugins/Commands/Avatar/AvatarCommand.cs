using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.SystemPrompt
{
    internal class AvatarCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("UserName")]
        public string UserName { get; set; }

        [Description("The avatar Url")]
        public string AvatarUrl { get; set; }
    }
}