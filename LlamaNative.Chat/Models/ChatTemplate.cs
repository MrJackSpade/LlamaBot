using LlamaBot.Shared.Utils;
using System.Text;

namespace LlamaNative.Chat.Models
{
    public class ChatTemplate
    {
        public string EndHeader { get; set; } = ": ";

        public string EndMessage { get; set; } = "\n";

        public string HeaderPadding { get; set; } = string.Empty;

        public string MessagePadding { get; set; } = string.Empty;

        public string NewHeaderPadding { get; set; } = string.Empty;

        public string StartHeader { get; set; } = "|";

        public int[] StopTokenIds { get; set; } = [];

        public string ToHeader(string userName, bool newHeader)
        {
            StringBuilder sb = new();

            sb.Append(StartHeader);
            sb.Append(userName);
            sb.Append(EndHeader);

            if (newHeader)
            {
                sb.Append(NewHeaderPadding);
            }
            else
            {
                sb.Append(HeaderPadding);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Writes a message to a string
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="endMessage">If true, append the end message characters and padding</param>
        /// <returns></returns>
        public string ToString(ChatMessage message, bool endMessage)
        {
            if (message.ContentOnly)
            {
                Ensure.NotNull(message.Content);
                return message.Content;
            }

            Ensure.NotNull(message.User);

            StringBuilder sb = new();

            sb.Append(this.ToHeader(message.User, false));

            sb.Append(message.Content);

            if (endMessage)
            {
                sb.Append(EndMessage);

                sb.Append(MessagePadding);
            }

            return sb.ToString();
        }
    }
}