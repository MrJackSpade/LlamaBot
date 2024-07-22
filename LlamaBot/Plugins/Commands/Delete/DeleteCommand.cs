using Discord.WebSocket;
using LlamaBot.Shared.Models;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.Delete
{
    internal class DeleteCommand(SocketSlashCommand command) : BaseCommand(command)
    {
        [Description("The message id to delete")]
        public ulong MessageId { get; set; }
    }
}