using Discord.WebSocket;
using LlamaBot.Discord.Model;

namespace LlamaBot.Discord.Commands
{
    internal class SystemPromptCommand : GenericCommand
    {
        public SystemPromptCommand(SocketSlashCommand command) : base(command)
        {
        }

        public string Prompt { get; set; }

        public bool ClearContext { get; set; }
    }
}