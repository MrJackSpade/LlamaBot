using Discord.WebSocket;
using LlamaBot.Discord.Model;
using System.ComponentModel.DataAnnotations;

namespace LlamaBot.Discord.Commands
{
    internal class UpdateCommand : BaseCommand
    {
        public UpdateCommand(SocketSlashCommand command) : base(command)
        {
        }

        [Required]
        public ulong MessageId { get; set; }

        [Required]
        public string Content { get; set; } 
    }
}