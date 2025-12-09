using LlamaBot.Extensions;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Commands.Name
{
    internal class NameCommandProvider : ICommandProvider<NameCommand>
    {
        private ILlamaBotClient? _llamaBotClient;

        public string Command => "name";

        public string Description => "Sets the name the model uses in place of your username";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(NameCommand command)
        {
            ulong channelId = command.Channel.GetChannelId();
            ulong userId = command.User.Id;

            ChannelSettingsCollection csi = _llamaBotClient.ChannelSettings;

            if (string.IsNullOrWhiteSpace(command.Username))
            {
               // If empty, maybe clear it? But the command param is required usually if not nullable. 
               // Assuming user wants to set it. 
               // The request implied "set the name", so let's set it.
               // We could allow clearing if they send a specific char, but standard behavior is usually overwrite.
               // Let's assume non-empty for now as per DTO.
            }

            csi.SetNameOverride(channelId, userId, command.Username);
            csi.SaveSettings(channelId);

            return CommandResult.Success($"Name override set to: {command.Username}");
        }

        public async Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;

            return InitializationResult.Success();
        }
    }
}
