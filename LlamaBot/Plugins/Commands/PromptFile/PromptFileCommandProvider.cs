using LlamaBot.Extensions;
using LlamaBot.Plugins.Commands.ClearContext;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Commands.PromptFile
{
    internal class PromptFileCommandProvider : ICommandProvider<PromptFileCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "promptfile";

        public string Description => "Updates or displays the bots system prompt (file only)";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(PromptFileCommand command)
        {
            if (command.ClearContext)
            {
                await _pluginService!.Command(new ClearContextCommand(command.Command)
                {
                    IncludeCache = true,
                });
            }

            ulong channelId = command.Channel.GetChannelId();

            ChannelSettingsCollection csi = _llamaBotClient.ChannelSettings;

            string? responseString;

            if (command.PromptFile is null)
            {
                responseString = csi.GetPrompt(channelId);
            }
            else
            {
                string promptContent = System.Text.Encoding.UTF8.GetString(command.PromptFile.Data);

                csi.SetPrompt(channelId, promptContent);

                csi.SaveSettings(channelId);

                responseString = "System Prompt Updated: " + promptContent;
            }

            return CommandResult.Success(System.Text.Encoding.UTF8.GetBytes(responseString), "Prompt.txt");
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
