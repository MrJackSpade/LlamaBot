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
        ChatMessage ReadResponse();
        void SendMessage(ChatMessage message);
    }
}
