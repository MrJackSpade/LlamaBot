using LlamaNative.Chat;

namespace LlamaBot
{
    public class Character
    {
        public CharacterMessage[] ChatMessages { get; set; } = [];

        public ChatSettings? ChatSettings { get; set; }

        public Dictionary<string, string> NameOverride { get; set; } = [];
    }
}