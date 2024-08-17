using Discord.WebSocket;
using LlamaBot.Shared.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LlamaBot.Plugins.Commands.Send
{
    internal class SendCommand : BaseCommand
    {
        public SendCommand(SocketSlashCommand command) : base(command)
        {
        }

        [Required]
        [Description("The content to send")]
        public string Content { get; set; }

        [Required]
        [Description("The user name to generate the message with")]
        public string UserName { get; set; }
    }
}