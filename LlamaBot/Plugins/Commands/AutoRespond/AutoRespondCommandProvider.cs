using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;
using LlamaNative.Utils;
using AutoRespondModel = LlamaNative.Chat.Models.AutoRespond;

namespace LlamaBot.Plugins.Commands.AutoRespond
{
    internal class AutoRespondCommandProvider : ICommandProvider<AutoRespondCommand>
    {
        private ILlamaBotClient? _llamaBotClient;

        public string Command => "autorespond";

        public string Description => "Changes the username that autoresponds to messages. Does not change the bot name";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(AutoRespondCommand command)
        {
            Ensure.NotNull(_llamaBotClient);

            ulong channelId = command.Channel.Id;

            bool disabled = command.UserName == "_";

            if (command.UserName is null)
            {
                AutoRespondModel autoRespond = _llamaBotClient.GetAutoRespond(channelId);

                if (string.IsNullOrWhiteSpace(autoRespond.UserName))
                {
                    return CommandResult.Success($"Default: {_llamaBotClient.BotName}");
                }

                if (autoRespond.UserName == "_")
                {
                    return CommandResult.Success("Disabled");
                }
                else
                {
                    return CommandResult.Success(autoRespond.UserName);
                }
            }

            _llamaBotClient.SetAutoRespond(channelId, command.UserName);

            return CommandResult.Success($"Autorespond set to: {(disabled ? "[DISABLED]" : command.UserName)}");
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;
            return InitializationResult.SuccessAsync();
        }
    }
}