using Discord.WebSocket;
using System.ComponentModel;

namespace LlamaBot.Plugins.Commands.Setting
{
    internal class SettingCommand(SocketSlashCommand command) : GenericCommand(command)
    {
        [Description("The setting name to modify (e.g., temperature, target, eta)")]
        public string Name { get; set; }

        [Description("The new value for the setting")]
        public string Value { get; set; }
    }
}