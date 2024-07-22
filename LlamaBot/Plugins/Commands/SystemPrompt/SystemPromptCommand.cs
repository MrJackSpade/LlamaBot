using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.SystemPrompt
{
    internal class SystemPromptCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("Optionally clears the bots memory")]
        public bool ClearContext { get; set; }

        [Description("The new system prompt")]
        public string Prompt { get; set; }
    }
}