using Discord.WebSocket;
using LlamaBot.Discord.Model;

namespace LlamaBot.Discord.Commands
{
    internal class ClearCommand : BaseCommand
    {
        public ClearCommand(SocketSlashCommand command) : base(command)
        {
        }
    }
}