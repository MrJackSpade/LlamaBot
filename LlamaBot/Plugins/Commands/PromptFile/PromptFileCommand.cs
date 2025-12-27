using Discord.WebSocket;
using LlamaBot.Shared.Models;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.PromptFile
{
    internal class PromptFileCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("Optionally clears the bots memory")]
        public bool ClearContext { get; set; }

        [Description("Upload a text file containing the prompt")]
        public CommandAttachment? PromptFile { get; set; }
    }
}
