using LlamaNative.Tokens.Models;
using System.Text.Json.Serialization;

namespace LlamaNative.Chat.Models
{
    public class ChatMessage
    {
        public ChatMessage(TokenMask contentMask, string? user, string? content, string? externalId = null)
        {
            ExternalId = externalId;
            Content = content;
            User = user;
            ContentMask = contentMask;
        }

        public ChatMessage(string user)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
        }

        [JsonConstructor]
        private ChatMessage()
        { }

        public string? Content { get; private set; }

        public TokenMask ContentMask { get; private set; }

        public bool ContentOnly { get; set; }

        public string? ExternalId { get; set; }

        public string? User { get; set; }
    }
}