using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaBotAutomated.Extensions;
using LlamaBotAutomated.Plugins.Commands.ClearContext;
using LlamaNative.Chat.Models;

namespace LlamaBotAutomated.Plugins.Commands.SystemPrompt
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

            if (command.Prompt is null)
            {
                responseString = csi.GetPrompt(channelId);
            }
            else
            {
                string prompt = command.Prompt.Replace("\\n", "\n");

                csi.SetPrompt(channelId, prompt);

                csi.SaveSettings(channelId);

                responseString = "System Prompt Updated: " + command.Prompt;
            }

            if (responseString.Length > 1995)
            {
                responseString = responseString[..1990] + "...";
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