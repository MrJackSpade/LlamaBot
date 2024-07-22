using Discord;

using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.Update
{
    internal class UpdateCommandProvider : ICommandProvider<UpdateCommand>
    {
        private IDiscordService? _discordClient;

        private IPluginService? _pluginService;

        public string Command => "update";

        public string Description => "Updates an existing message";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(UpdateCommand command)
        {
            if (command.Channel is ISocketMessageChannel smc)
            {
                IMessage message = await smc.GetMessageAsync(command.MessageId);

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
            return InitializationResult.SuccessAsync();
        }
    }
}