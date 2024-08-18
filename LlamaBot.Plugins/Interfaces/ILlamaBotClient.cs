using Discord;
using Discord.WebSocket;
using LlamaNative.Chat.Models;

namespace LlamaBot.Plugins.Interfaces
{
    public interface ILlamaBotClient
    {
        string BotName { get; }

        string SystemPrompt { get; set; }

        void Clear(bool v);

        Task<string> GenerateMessageBody(ISocketMessageChannel smc, string displayName);

        AutoRespond GetAutoRespond(ulong channelId);

        void SetAutoRespond(ulong channelId, string username, bool disabled);

        void SetClearDate(ulong channelId, DateTime triggered);

        Task<IMessage?> TryGetLastBotMessage(ISocketMessageChannel channel);

        void TryInterrupt();

        void TryProcessMessageAsync(ISocketMessageChannel smc, ReadResponseSettings readResponseSettings);
    }
}