using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Utils;
using Newtonsoft.Json;

namespace LlamaBot.Plugins.Commands.Tokenize
{
    internal class TokenizeCommandProvider : ICommandProvider<TokenizeCommand>
    {
        private ILlamaBotClient? _llamaBotClient;

        public string Command => "tokenize";

        public string Description => "tokenizes a string";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(TokenizeCommand command)
        {
            Ensure.NotNull(_llamaBotClient);

            if (command.Channel is ISocketMessageChannel smc)
            {
                Dictionary<string, int> results = _llamaBotClient.Tokenize(command.Content);

                string json = JsonConvert.SerializeObject(results, Formatting.Indented);

                return CommandResult.Success(json);
            }
            else
            {
                return CommandResult.Error($"Requested channel is not {nameof(ISocketMessageChannel)}");
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;
            return InitializationResult.SuccessAsync();
        }
    }
}