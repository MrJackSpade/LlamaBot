using LlamaNative.Chat;
using LlamaNative.Chat.Models;

namespace LlamaBot
{
    public class Character
    {
        public ChatSettings ChatSettings { get; set; }

        public ChatMessage[] ChatMessages { get; set; } = [];
    }
}
