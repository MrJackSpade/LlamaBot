namespace LlamaNative.Chat.Models
{
    public class ChatMessage
    {
        public ChatMessage(string user, string? content, string? externalId = null)
        {
            ExternalId = externalId;
            Content = content;
            User = user ?? throw new ArgumentNullException(nameof(user));
        }

        public ChatMessage(string user)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
        }

        public string? Content { get; set; }

        public string? ExternalId { get; set; }

        public string User { get; set; }
    }
}