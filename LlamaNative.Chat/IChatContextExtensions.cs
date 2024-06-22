using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;

namespace LlamaNative.Chat
{
    public static class IChatContextExtensions
    {
        public static void SendMessage(this IChatContext context, string username, string message, string? externalId = null)
        {
            ChatMessage chatMessage = new()
            {
                User = username,
                Content = message,
                ExternalId = externalId
            };

            context.SendMessage(chatMessage);
        }
    }
}
