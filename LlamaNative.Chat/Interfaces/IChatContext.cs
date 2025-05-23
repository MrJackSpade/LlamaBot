﻿using LlamaNative.Chat.Models;
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

        string PredictNextUser();

        List<ChatMessage> ReadResponse(ReadResponseSettings responseSettings);

        void RemoveAt(int index);

        void SendMessage(ChatMessage message);

        bool TryInterrupt();

        List<Token> Tokenize(string content);
    }
}