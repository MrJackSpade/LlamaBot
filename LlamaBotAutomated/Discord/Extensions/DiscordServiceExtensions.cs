using LlamaBot.Shared.Models;
using LlamaBotAutomated.Discord;

namespace LlamaBotAutomated.Discord.Extensions
{
    public static class DiscordServiceExtensions
    {
        public static async Task AddCommand<T>(this DiscordService service, string command, string description, Func<T, Task<CommandResult>> action, params SlashCommandOption[] slashCommandOptions) where T : BaseCommand
        {
            await service.AddCommand(command, description, typeof(T), a => action.Invoke((T)a), slashCommandOptions);
        }
    }
}