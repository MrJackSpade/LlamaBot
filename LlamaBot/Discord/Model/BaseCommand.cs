using Discord;
using Discord.WebSocket;

namespace LlamaBot.Discord.Model
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