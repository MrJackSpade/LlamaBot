using System.Text;

namespace LlamaNative.Chat.Models
{
    public class ChatTemplate
    {
        public string EndHeader { get; set; } = ": ";

        public string EndMessage { get; set; } = "\n";

        public bool HeaderNewline { get; set; } = false;

        public bool MessageNewline { get; set; } = false;

        public string StartHeader { get; set; } = "|";

        public string ToHeader(string userName)
        {
            StringBuilder sb = new();
            sb.Append(StartHeader);
            sb.Append(userName);
            sb.Append(EndHeader);

            if (HeaderNewline)
            {
                sb.Append('\n');
            }

            return sb.ToString();
        }

        public string ToString(ChatMessage message)
        {
            StringBuilder sb = new();
            sb.Append(this.ToHeader(message.User));
            sb.Append(message.Content);
            sb.Append(EndMessage);

            if (MessageNewline)
            {
                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}