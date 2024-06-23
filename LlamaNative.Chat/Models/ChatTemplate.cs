namespace LlamaNative.Chat.Models
{
    public class ChatTemplate
    {
        public string EndHeader { get; set; } = ": ";

        public string EndMessage { get; set; } = "\n";

        public bool HeaderNewline { get; set; } = false;

        public bool MessageNewline { get; set; } = false;

        public string StartHeader { get; set; } = "|";
    }
}