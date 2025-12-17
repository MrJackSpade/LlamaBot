using LlamaBot.Extensions;
using LlamaBot.Plugins.Commands.ClearContext;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Commands.SystemPrompt
{
    internal class SystemPromptCommandProvider : ICommandProvider<SystemPromptCommand>
    {
        private IDiscordService? _discordClient;

        private ILlamaBotClient? _llamaBotClient;

        private IPluginService? _pluginService;

        public string Command => "prompt";

        public string Description => "Updates or displays the bots system prompt";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(SystemPromptCommand command)
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

            // Determine prompt source: file takes priority over string
            string? promptContent = null;
            if (command.PromptFile is not null)
            {
                promptContent = System.Text.Encoding.UTF8.GetString(command.PromptFile.Data);
            }
            else if (command.Prompt is not null)
            {
                promptContent = command.Prompt.Replace("\\n", "\n");
            }

            if (promptContent is null)
            {
                responseString = csi.GetPrompt(channelId);
            }
            else
            {
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