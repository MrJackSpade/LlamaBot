using Discord;
using Discord.WebSocket;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;
using System.Text;

namespace LlamaBot.Plugins.Commands.Download
{
    internal class DownloadCommandProvider : ICommandProvider<DownloadCommand>
    {
        private ILlamaBotClient? _llamaBotClient;

        public string Command => "download";

        public string Description => "Download the last response as a text file";

        public SlashCommandOption[] SlashCommandOptions => [];

        public async Task<CommandResult> OnCommand(DownloadCommand command)
        {
            if (_llamaBotClient == null)
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

            if (messages.Count == 0)
            {
                return CommandResult.Error("No messages found to download");
            }

            // Reverse to get chronological order (oldest first)
            messages.Reverse();

            // Get the first message ID for the filename
            ulong firstMessageId = messages[0].Id;

            // Concatenate all message content
            StringBuilder content = new();
            foreach (IMessage message in messages)
            {
                ParsedMessage parsed = _llamaBotClient.ParseMessage(message);

                if (content.Length > 0)
                {
                    content.AppendLine();
                    content.AppendLine();
                }

                content.Append(parsed.Content);
            }

            // Convert to bytes and return as file
            byte[] fileData = Encoding.UTF8.GetBytes(content.ToString());
            string fileName = $"{firstMessageId}.txt";

            return CommandResult.Success(fileData, fileName);
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;
            return InitializationResult.SuccessAsync();
        }
    }
}
