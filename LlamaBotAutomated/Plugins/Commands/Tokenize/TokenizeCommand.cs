using Discord.WebSocket;
using LlamaBot.Shared.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LlamaBotAutomated.Plugins.Commands.Tokenize
{
    internal class TokenizeCommand(SocketSlashCommand command) : BaseCommand(command)
    {
        [Required]
        [Description("The content to tokenize")]
        public string Content { get; set; }
    }
}