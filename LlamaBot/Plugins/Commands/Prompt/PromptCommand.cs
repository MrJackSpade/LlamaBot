using Discord.WebSocket;
using LlamaBot.Shared.Models;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.Prompt
{
    internal class PromptCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("Optionally clears the bots memory")]
        public bool ClearContext { get; set; }

        [Description("The new system prompt")]
        public string Prompt { get; set; }
    }
}
