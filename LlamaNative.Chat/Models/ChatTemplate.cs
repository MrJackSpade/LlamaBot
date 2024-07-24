using System.Text;

namespace LlamaNative.Chat.Models
{
    public class ChatTemplate
    {
        public string EndHeader { get; set; } = ": ";

        public string EndMessage { get; set; } = "\n";

        public string HeaderPadding { get; set; } = string.Empty;

        public string NewHeaderPadding { get; set; } = string.Empty;

        public string MessagePadding { get; set; } = string.Empty;

        public string StartHeader { get; set; } = "|";

        public string ToHeader(string userName, bool newHeader)
        {
            StringBuilder sb = new();
            sb.Append(StartHeader);
            sb.Append(userName);
            sb.Append(EndHeader);

            if (newHeader)
            {
                sb.Append(NewHeaderPadding);
            } else
            {
                sb.Append(HeaderPadding);
            }
            
            return sb.ToString();
        }

        public string ToString(ChatMessage message)
        {
            StringBuilder sb = new();
            sb.Append(this.ToHeader(message.User, false));
            sb.Append(message.Content);
            sb.Append(EndMessage);
            sb.Append(MessagePadding);

            return sb.ToString();
        }
    }
}