using LlamaNative.Tokens.Models;
using LlamaNative.Utils;
using System.Text;

namespace LlamaNative.Chat.Models
{
    public enum HeaderType
    {
        User, Assistant, Generic
    }

    public class ChatTemplate
    {
        public string EndHeader { get; set; } = ": ";

        public string EndMessage { get; set; } = "\n";

        public string MessagePrefix { get; set; } = string.Empty;

        public string MessageSuffix { get; set; } = string.Empty;

        public string NewHeaderPadding { get; set; } = string.Empty;

        public string StartHeader { get; set; } = "|";

        public string StartUserHeader
        {
            get => _startUserHeader ?? StartHeader;
            set => _startUserHeader = value;
        }

        public string StartAssistantHeader
        {
            get => _startAssistantHeader ?? StartHeader;
            set => _startAssistantHeader = value;
        }

        private string _startAssistantHeader = string.Empty;

        private string _startUserHeader = string.Empty;

        public int[] StopTokenIds { get; set; } = [];

        public MaskedString ToHeader(string userName, bool newHeader, HeaderType headerType = HeaderType.Generic)
        {
            StringBuilder sb = new();

            switch (headerType)
            {
                case HeaderType.User:
                    sb.Append(StartUserHeader); 
                    break;
                case HeaderType.Assistant:
                    sb.Append(StartAssistantHeader); 
                    break;
                case HeaderType.Generic:
                    sb.Append(StartHeader);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(headerType), headerType, null);
            }

            sb.Append(userName);
            sb.Append(EndHeader);

            if (newHeader)
            {
                sb.Append(NewHeaderPadding);
            }

            return new MaskedString(sb.ToString(), TokenMask.Template);
        }

        /// <summary>
        /// Writes a message to a string
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="endMessage">If true, append the end message characters and padding</param>
        /// <returns></returns>
        public IEnumerable<MaskedString> ToMaskedString(ChatMessage message, bool endMessage, HeaderType headerType = HeaderType.Generic)
        {
            if (message.ContentOnly)
            {
                Ensure.NotNull(message.Content);
                string content = message.Content;

                yield return new MaskedString(content, message.ContentMask);
                yield break;
            }

            Ensure.NotNull(message.User);

            yield return this.ToHeader(message.User, false, headerType);

            yield return new MaskedString(MessagePrefix + message.Content, message.ContentMask);

            if (endMessage)
            {
                yield return new MaskedString(EndMessage + MessageSuffix, TokenMask.Template);
            }
        }

        public string ToString(ChatMessage message, bool endMessage, HeaderType headerType = HeaderType.Generic)
        {
            StringBuilder sb = new();

            foreach (MaskedString ms in this.ToMaskedString(message, endMessage, headerType))
            {
                sb.Append(ms.Value);
            }

            return sb.ToString();
        }
    }
}