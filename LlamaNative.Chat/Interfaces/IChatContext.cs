using LlamaNative.Chat.Models;

namespace LlamaNative.Chat.Interfaces
{
    public interface IChatContext
    {
        int Count { get; }
        uint AvailableBuffer { get; }

        ChatMessage this[int index] { get; }

        uint CalculateLength(ChatMessage message);

        void Clear();

        void Insert(int index, ChatMessage message);

        ChatMessage ReadResponse();

        void RemoveAt(int index);

        void SendMessage(ChatMessage message);
        string PredictNextUser();
    }
}