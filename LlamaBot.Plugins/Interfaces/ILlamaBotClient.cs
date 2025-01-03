using Discord;
using Discord.WebSocket;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Interfaces
{
    public interface ILlamaBotClient
    {
        string BotName { get; }

        string DefaultSystemPrompt { get; }

        Dictionary<ulong, string> SystemPrompts { get; set; }

        string? BuildMessage(string author, string? content);

        void Clear(bool v);

        Task<string> GenerateMessageBody(ISocketMessageChannel smc, string displayName);

        AutoRespond GetAutoRespond(ulong channelId);

        ParsedMessage ParseMessage(IMessage checkMessage);

        void SetAutoRespond(ulong channelId, string username, bool disabled);

        void SetClearDate(ulong channelId, DateTime triggered);

        Dictionary<string, int> Tokenize(string content);

        Task<IMessage?> TryGetLastBotMessage(ISocketMessageChannel channel);

        void TryInterrupt();

        void TryProcessMessageAsync(ISocketMessageChannel smc, ReadResponseSettings readResponseSettings);
    }
}