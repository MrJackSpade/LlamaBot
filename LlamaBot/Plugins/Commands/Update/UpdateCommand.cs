using Discord.WebSocket;
using LlamaBot.Shared.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LlamaBot.Plugins.Commands.Update
{
    internal class UpdateCommand(SocketSlashCommand command) : BaseCommand(command)
    {
        [Required]
        [Description("The new message content")]
        public string? Content { get; set; }

        [Required]
        [Description("The message id to update")]
        public ulong MessageId { get; set; }
    }
}