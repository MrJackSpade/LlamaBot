using Discord.WebSocket;
using LlamaBot.Shared.Models;

namespace LlamaBotAutomated.Plugins.Commands
{
    internal class GenericCommand(SocketSlashCommand command) : BaseCommand(command)
    {
    }
}