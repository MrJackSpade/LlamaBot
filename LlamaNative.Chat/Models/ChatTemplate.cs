namespace LlamaNative.Chat.Models
{
    public class ChatTemplate
    {
        public string EndHeader { get; set; } = ": ";
        public string EndMessage { get; set; } = "\n";
        public string StartHeader { get; set; } = "|";
    }
}