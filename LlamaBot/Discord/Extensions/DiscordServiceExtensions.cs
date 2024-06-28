﻿using LlamaBot.Discord.Model;

namespace LlamaBot.Discord.Extensions
{
    public static class DiscordServiceExtensions
    {
        public static async Task AddCommand<T>(this DiscordClient service, string command, string description, Func<T, Task<CommandResult>> action, params SlashCommandOption[] slashCommandOptions) where T : BaseCommand
        {
            await service.AddCommand(command, description, typeof(T), a => action.Invoke((T)a), slashCommandOptions);
        }
    }
}