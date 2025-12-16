using LlamaBot.Plugins.Commands.SystemPrompt;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;

namespace LlamaBot.Plugins.Commands.Avatar
{
    internal class AvatarCommandProvider : ICommandProvider<AvatarCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "avatar";

        public string Description => "Updates a bots avatar";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(AvatarCommand command)
        {
            _discordClient!.SetAvatarUrl(command.Channel.Id, command.UserName, command.AvatarUrl);

            return CommandResult.Success("Avatar updated");
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