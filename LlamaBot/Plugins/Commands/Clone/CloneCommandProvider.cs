using LlamaBot.Extensions;
using LlamaBot.Plugins.Commands.ClearContext;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Commands.Clone
{
    internal class CloneCommandProvider : ICommandProvider<CloneCommand>
    {
        private IDiscordService? _discordClient;
        private ILlamaBotClient? _llamaBotClient;
        private IPluginService? _pluginService;

        public string Command => "clone";

        public string Description => "Clones a channel";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(CloneCommand command)
        {
            ulong channelId = command.Channel.GetChannelId();

            ChannelSettingsCollection csi = _llamaBotClient.ChannelSettings;

            ChannelSettings? sourceSettings = csi.GetValue(command.ChannelId);

            if (sourceSettings is null)
            {
                return CommandResult.Error("Source channel has no settings to clone.");
            }

            csi.AddOrUpdate(channelId, sourceSettings.Clone());

            csi.SaveSettings(channelId);

            return CommandResult.Success("Channel settings cloned successfully.");
        }

        public async Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _pluginService = args.PluginService;
            _discordClient = args.DiscordService;
            _llamaBotClient = args.LlamaBotClient;

            return InitializationResult.Success();
        }
    }
}