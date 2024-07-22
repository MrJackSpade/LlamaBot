using Discord;

using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.Delete
{
    internal class DeleteCommandProvider : ICommandProvider<DeleteCommand>
    {
        private IDiscordService? _discordClient;

        private IPluginService? _pluginService;

        public string Command => "delete";

        public string Description => "Deletes a specific bot message";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(DeleteCommand command)
        {
            if (command.Channel is ISocketMessageChannel smc)
            {
                IMessage message = await smc.GetMessageAsync(command.MessageId);
                await message.DeleteAsync();
                await command.Command.DeleteOriginalResponseAsync();
                return CommandResult.Success();
            }
            else
            {
                return CommandResult.Error($"Requested channel is not {nameof(ISocketMessageChannel)}");
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _pluginService = args.PluginService;
            _discordClient = args.DiscordService;
            return InitializationResult.SuccessAsync();
        }
    }
}