using LlamaNative.Chat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Chat.Interfaces
{
    public interface IChatContext
    {
        int Count { get; }

        ChatMessage this[int index] { get; }

        ChatMessage ReadResponse();

        void RemoveAt(int index);

        void SendMessage(ChatMessage message);
    }
}