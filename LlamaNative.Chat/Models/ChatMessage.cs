using LlamaNative.Tokens.Models;
using System.Text.Json.Serialization;

namespace LlamaNative.Chat.Models
{
    public class ChatMessage
    {
        public ChatMessage(TokenMask contentMask, string? user, string? content)
        {
            Content = content;
            User = user;
            ContentMask = contentMask;
        }

        public string? Content { get; private set; }

        public TokenMask ContentMask { get; private set; }

        public bool ContentOnly { get; set; }

        public string? User { get; set; }
    }
}