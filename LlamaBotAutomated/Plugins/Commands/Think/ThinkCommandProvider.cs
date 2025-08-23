using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using LlamaBotAutomated.Extensions;
using LlamaNative.Chat.Models;

namespace LlamaBotAutomated.Plugins.Commands.Think
{
    internal class ThinkCommandCommandProvider : ICommandProvider<ThinkCommand>
    {
        private ILlamaBotClient? _llamaBotClient;

        public string Command => "think";

        public string Description => "Updates or displays the bots forced thoughts";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(ThinkCommand command)
        {
            ulong channelId = command.Channel.GetChannelId();

            string? responseString;

            ChannelSettingsCollection csi = _llamaBotClient.ChannelSettings;

            if (command.Think is null)
            {
                responseString = csi.GetUserThoughts(channelId, command.UserName);
            }
            else
            {
                string think = command.Think.Replace("\\n", "\n");

                csi.SetThoughts(channelId, command.UserName, think);

                csi.SaveSettings(channelId);

                responseString = "Thoughts Updated: " + command.Think;
            }

            if (responseString.Length > 1995)
            {
                responseString = responseString[..1990] + "...";
            }

            return CommandResult.Success(responseString);
        }

        public async Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;

            return InitializationResult.Success();
        }
    }
}