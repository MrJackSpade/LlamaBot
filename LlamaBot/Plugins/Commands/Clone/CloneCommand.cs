using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.Clone
{
    internal class CloneCommand(SocketSlashCommand command) : GenericCommand(command)
    {

        [Description("The channel to clone from")]
        public ulong ChannelId { get; set; }
    }
}