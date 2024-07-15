using Discord.WebSocket;
using LlamaBot.Discord.Model;

namespace LlamaBot.Discord.Commands
{
    internal class GenericCommand : BaseCommand
    {
        public GenericCommand(SocketSlashCommand command) : base(command)
        {
        }
    }
}