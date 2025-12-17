using Discord;
using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;
using System.Text;

namespace LlamaBot.Plugins.Commands.Resend
{
    internal class ResendCommandProvider : ICommandProvider<ResendCommand>
    {
        private const int MAX_MESSAGE_LENGTH = 1950;

        private IDiscordService? _discordService;

        private ILlamaBotClient? _llamaBotClient;

        public string Command => "resend";

        public string Description => "Re-splits and resends the last response with smarter line breaks";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(ResendCommand command)
        {
            if (_llamaBotClient == null || _discordService == null)
            {
                return CommandResult.Error("Command provider not initialized");
            }

            if (command.Channel is not ISocketMessageChannel smc)
            {
                return CommandResult.Error($"Requested channel is not {nameof(ISocketMessageChannel)}");
            }

            // Get the last bot message
            IMessage? rootMessage = await _llamaBotClient.TryGetLastBotMessage(smc);

            if (rootMessage is null)
            {
                return CommandResult.Error("No bot message found");
            }

            ParsedMessage parsedRootMessage = _llamaBotClient.ParseMessage(rootMessage);
            string responseAuthor = parsedRootMessage.Author;

            // Collect all consecutive messages from the same author
            List<IMessage> messages = [];

            foreach (IMessage checkMessage in await smc.GetMessagesAsync(500).FlattenAsync())
            {
                if (checkMessage.Type == MessageType.ApplicationCommand)
                {
                    continue;
                }

                ParsedMessage parsed = _llamaBotClient.ParseMessage(checkMessage);

                if (parsed.Author == responseAuthor)
                {
                    messages.Add(checkMessage);
                }
                else
                {
                    break;
                }
            }

            // Only resend if there was more than one message (truncated response)
            if (messages.Count <= 1)
            {
                return CommandResult.Error("Last response was not truncated (only one message). No need to resend.");
            }

            // Reverse to get chronological order (oldest first)
            messages.Reverse();

            // Concatenate all message content
            StringBuilder fullContent = new();
            foreach (IMessage message in messages)
            {
                ParsedMessage parsed = _llamaBotClient.ParseMessage(message);
                fullContent.Append(parsed.Content);
            }

            string content = fullContent.ToString();

            // Delete the original messages
            foreach (IMessage message in messages)
            {
                await message.DeleteAsync();
            }

            // Delete the command response
            await command.Command.DeleteOriginalResponseAsync();

            // Re-send with smart splitting
            foreach (string chunk in SmartSplit(content, MAX_MESSAGE_LENGTH))
            {
                await _discordService.SendMessageAsync(smc, chunk, responseAuthor);
            }

            return CommandResult.Success();
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;
            _discordService = args.DiscordService;
            return InitializationResult.SuccessAsync();
        }

        /// <summary>
        /// Splits content into chunks of maxLength or less, preferring to break on:
        /// 1. Double newlines (\n\n)
        /// 2. Single newlines (\n)
        /// 3. Arbitrary position (last resort)
        /// </summary>
        private static IEnumerable<string> SmartSplit(string content, int maxLength)
        {
            while (content.Length > 0)
            {
                if (content.Length <= maxLength)
                {
                    yield return content;
                    break;
                }

                // Try to find the best split point within maxLength
                int splitIndex = FindBestSplitPoint(content, maxLength);
                
                string chunk = content[..splitIndex].TrimEnd();
                yield return chunk;

                // Move past the split point, trimming any leading whitespace
                content = content[splitIndex..].TrimStart();
            }
        }

        /// <summary>
        /// Finds the best split point within maxLength characters.
        /// Prefers \n\n, then \n, then arbitrary.
        /// </summary>
        private static int FindBestSplitPoint(string content, int maxLength)
        {
            string searchArea = content[..maxLength];

            // Try to find the last \n\n within the search area
            int doubleNewline = searchArea.LastIndexOf("\n\n", StringComparison.Ordinal);
            if (doubleNewline > 0)
            {
                // Return position after the double newline
                return doubleNewline + 2;
            }

            // Try to find the last \n within the search area
            int singleNewline = searchArea.LastIndexOf('\n');
            if (singleNewline > 0)
            {
                // Return position after the newline
                return singleNewline + 1;
            }

            // No good split point found, use arbitrary split at maxLength
            return maxLength;
        }
    }
}
