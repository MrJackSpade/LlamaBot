using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Tokens.Models;
using LlamaNative.Utils;
using Newtonsoft.Json;
using System.Text;

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
                StringBuilder sb = new();

                sb.AppendLine("```json");
                sb.AppendLine("{ ");

                List<Token> results = _llamaBotClient.Tokenize(command.Content);

                for (int i = 0; i < results.Count; i++)
                {
                    Token t = results[i];
                    sb.Append("    \"" + t.GetEscapedValue().Replace("*", "\\*") + "\": " + t.Id);

                    if (i < results.Count - 1)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }

                sb.AppendLine("}");
                sb.AppendLine("```");

                return CommandResult.Success(sb.ToString());
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