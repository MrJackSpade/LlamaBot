using Discord;

using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaBot.Shared.Utils;

namespace LlamaBot.Plugins.Commands.Update
{
    internal class UpdateCommandProvider : ICommandProvider<UpdateCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "update";

        public string Description => "Updates an existing message";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(UpdateCommand command)
        {
            Ensure.NotNull(_llamaBotClient);

            IMessage? message = null;

            if (command.Channel is ISocketMessageChannel smc)
            {
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

                if (message is IUserMessage um)
                {
                    await um.ModifyAsync(m => m.Content = command.Content);
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
            _pluginService = args.PluginService;
            _discordClient = args.DiscordService;
            _llamaBotClient = args.LlamaBotClient;
            return InitializationResult.SuccessAsync();
        }
    }
}