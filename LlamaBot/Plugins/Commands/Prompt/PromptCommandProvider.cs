using LlamaBot.Extensions;
using LlamaBot.Plugins.Commands.ClearContext;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Commands.Prompt
{
    internal class PromptCommandProvider : ICommandProvider<PromptCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "prompt";

        public string Description => "Updates or displays the bots system prompt (text only)";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(PromptCommand command)
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

            if (command.Prompt is null)
            {
                responseString = csi.GetPrompt(channelId);
            }
            else
            {
                string promptContent = command.Prompt.Replace("\\n", "\n");

                csi.SetPrompt(channelId, promptContent);

                csi.SaveSettings(channelId);

                responseString = "System Prompt Updated: " + promptContent;
            }

            return CommandResult.Success(responseString);
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
