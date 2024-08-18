using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using LlamaBot.Shared.Utils;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Commands.ClearContext
{
    internal class AutoRespondCommandProvider : ICommandProvider<AutoRespondCommand>
    {
        private ILlamaBotClient? _llamaBotClient;

        public string Command => "autorespond";

        public string Description => "Changes the username that autoresponds to messages. Does not change the bot name";

        public SlashCommandOption[] SlashCommandOptions => [];

        public Task<CommandResult> OnCommand(AutoRespondCommand command)
        {
            Ensure.NotNull(_llamaBotClient);

            ulong channelId = command.Channel.Id;

            if (command.UserName is null)
            {
                AutoRespond autoRespond = _llamaBotClient.GetAutoRespond(channelId);

                if (string.IsNullOrWhiteSpace(autoRespond.UserName))
                {
                    return CommandResult.SuccessAsync($"Default: {_llamaBotClient.BotName}");
                }

                if (autoRespond.Disabled)
                {
                    return CommandResult.SuccessAsync("Disabled");
                }

                return CommandResult.SuccessAsync(autoRespond.UserName);
            }

            _llamaBotClient.SetAutoRespond(channelId, command.UserName, command.Disabled);

            return CommandResult.SuccessAsync();
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;
            return InitializationResult.SuccessAsync();
        }
    }
}