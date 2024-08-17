using Discord.WebSocket;
using LlamaBot.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaBot.Plugins.Commands.Generate
{
    public class GenerateCommand : BaseCommand
    {
        public GenerateCommand(SocketSlashCommand command) : base(command)
        {
        }

        [Required]
        [Description("The user name to generate the message with")]
        public string UserName { get; set; }
    }
}