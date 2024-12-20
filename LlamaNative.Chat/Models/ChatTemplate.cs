﻿using LlamaNative.Tokens.Models;
using LlamaNative.Utils;
using System.Text;

namespace LlamaNative.Chat.Models
{
    public class ChatTemplate
    {
        public string EndHeader { get; set; } = ": ";

        public string EndMessage { get; set; } = "\n";

        public string MessagePrefix { get; set; } = string.Empty;

        public string MessageSuffix { get; set; } = string.Empty;

        public string NewHeaderPadding { get; set; } = string.Empty;

        public string StartHeader { get; set; } = "|";

        public int[] StopTokenIds { get; set; } = [];

        public MaskedString ToHeader(string userName, bool newHeader)
        {
            StringBuilder sb = new();

            sb.Append(StartHeader);
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
        public IEnumerable<MaskedString> ToMaskedString(ChatMessage message, bool endMessage)
        {
            if (message.ContentOnly)
            {
                Ensure.NotNull(message.Content);
                string content = message.Content;

                yield return new MaskedString(content, message.ContentMask);
                yield break;
            }

            Ensure.NotNull(message.User);

            yield return this.ToHeader(message.User, false);

            yield return new MaskedString(MessagePrefix + message.Content, message.ContentMask);

            if (endMessage)
            {
                yield return new MaskedString(EndMessage + MessageSuffix, TokenMask.Template);
            }
        }

        public string ToString(ChatMessage message, bool endMessage)
        {
            StringBuilder sb = new();

            foreach (MaskedString ms in this.ToMaskedString(message, endMessage))
            {
                sb.Append(ms.Value);
            }

            return sb.ToString();
        }
    }
}