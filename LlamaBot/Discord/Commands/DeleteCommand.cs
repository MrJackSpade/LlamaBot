using Discord.WebSocket;
using LlamaBot.Discord.Model;

namespace LlamaBot.Discord.Commands
{
    internal class DeleteCommand : BaseCommand
    {
        public DeleteCommand(SocketSlashCommand command) : base(command)
        {
        }

        public ulong MessageId { get; set; }
    }
}