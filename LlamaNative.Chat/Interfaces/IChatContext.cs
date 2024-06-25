using LlamaNative.Chat.Models;

namespace LlamaNative.Chat.Interfaces
{
    public interface IChatContext
    {
        uint AvailableBuffer { get; }

        int Count { get; }

        int MessageCount { get; }

        ChatMessage this[int index] { get; }

        uint CalculateLength(ChatMessage message);

        void Clear();

        void Insert(int index, ChatMessage message);

        string PredictNextUser();

        IEnumerable<ChatMessage> ReadResponse();

        void RemoveAt(int index);

        void SendMessage(ChatMessage message);
    }
}