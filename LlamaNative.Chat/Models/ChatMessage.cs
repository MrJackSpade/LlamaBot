using System.Text.Json.Serialization;

namespace LlamaNative.Chat.Models
{
    public class ChatMessage
    {
        public ChatMessage(string? user, string? content, string? externalId = null)
        {
            ExternalId = externalId;
            Content = content;
            User = user;
        }

        public ChatMessage(string user)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
        }

        [JsonConstructor]
        private ChatMessage()
        { }

        public string? Content { get; set; }

        public bool ContentOnly { get; set; }

        public string? ExternalId { get; set; }

        public string? User { get; set; }
    }
}