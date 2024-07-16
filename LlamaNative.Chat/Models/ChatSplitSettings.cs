namespace LlamaNative.Chat.Models
{
    public class ChatSplitSettings
    {
        public bool DoubleNewlineSplit { get; set; } = true;

        public int MessageMaxCharacters { get; set; } = 500;

        public int MessageMinTokens { get; set; } = 10;

        public int MessageSplitId { get; set; } = 0;
    }
}