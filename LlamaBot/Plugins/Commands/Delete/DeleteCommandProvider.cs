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

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "delete";

        public string Description => "Deletes a specific bot message";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(DeleteCommand command)
        {
            if (command.Channel is ISocketMessageChannel smc)
            {
                IMessage? message = null;

                if (command.MessageId == 0)
                {
                    message = await _llamaBotClient.TryGetLastBotMessage(smc);

                    if (message == null)
                    {
                        return CommandResult.Error("Last found message does not belong to bot. Please provide message id");
                    }
                }
                else
                {
                    message = await smc.GetMessageAsync(command.MessageId);
                }

                if (message != null)
                {
                    await message.DeleteAsync();
                }

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
            _llamaBotClient = args.LlamaBotClient;
            _pluginService = args.PluginService;
            _discordClient = args.DiscordService;
            return InitializationResult.SuccessAsync();
        }
    }
}