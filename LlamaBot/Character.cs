using LlamaNative.Chat;
using LlamaNative.Chat.Models;

namespace LlamaBot
{
    public class Character
    {
        public ChatMessage[] ChatMessages { get; set; } = [];

        public ChatSettings? ChatSettings { get; set; }

        public Dictionary<string, string> NameOverride { get; set; } = [];
    }
}