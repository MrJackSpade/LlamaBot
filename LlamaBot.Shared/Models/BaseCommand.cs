using Discord;
using Discord.WebSocket;

namespace LlamaBot.Shared.Models
{
    public class BaseCommand
    {
        public BaseCommand(SocketSlashCommand command)
        {
            Command = command;
        }

        public IChannel Channel => Command.Channel;

        public SocketSlashCommand Command { get; }

        public SocketUser? User => Command.User;
    }
}