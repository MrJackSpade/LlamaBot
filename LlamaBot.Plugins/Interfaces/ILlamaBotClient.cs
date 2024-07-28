using Discord;
using Discord.WebSocket;

namespace LlamaBot.Plugins.Interfaces
{
    public interface ILlamaBotClient
    {
        string SystemPrompt { get; set; }

        void Clear(bool v);

        void SetClearDate(ulong channelId, DateTime triggered);

        Task<IMessage?> TryGetLastBotMessage(ISocketMessageChannel channel);

        void TryInterrupt();

        void TryProcessMessageThread(ISocketMessageChannel smc, bool continueLast);
    }
}