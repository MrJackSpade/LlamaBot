using Discord.WebSocket;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands
{
    internal class GenericCommand(SocketSlashCommand command) : BaseCommand(command)
    {
    }
}