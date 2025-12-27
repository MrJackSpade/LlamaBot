using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.Download
{
    internal class DownloadCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("Download the full chat history back to the last clear")]
        public bool FullHistory { get; set; }
    }
}
