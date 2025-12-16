using LlamaNative.Chat.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Chat.Interfaces
{
    public interface IChatContext
    {
        uint AvailableBuffer { get; }

        int Count { get; }

        int MessageCount { get; }

        ChatMessage this[int index] { get; }

        void Clear(bool includeCache);

        void Insert(int index, ChatMessage message);

        /// <summary>
        /// Predicts the next user based on the current context.
        /// </summary>
        /// <param name="samplerSettings">The sampler settings to use for prediction.</param>
        string PredictNextUser(object samplerSettings);

        List<ChatMessage> ReadResponse(ReadResponseSettings responseSettings, CancellationToken cancellationToken);

        void RemoveAt(int index);

        void SendMessage(ChatMessage message);

        List<Token> Tokenize(string content);

        bool TryInterrupt();
    }
}