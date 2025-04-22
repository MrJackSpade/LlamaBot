using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Chat.Extensions
{
    public static class IChatContextExtensions
    {
        public static void SendContent(this IChatContext context, TokenMask contentMask, string message)
        {
            ChatMessage chatMessage = new(contentMask, null, message)
            {
                ContentOnly = true
            };

            context.SendMessage(chatMessage);
        }

        public static void SendMessage(this IChatContext context, TokenMask contentMask, string username, string message)
        {
            ChatMessage chatMessage = new(contentMask, username, message);

            context.SendMessage(chatMessage);
        }
    }
}