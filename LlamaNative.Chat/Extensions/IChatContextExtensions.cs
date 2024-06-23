using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;

namespace LlamaNative.Chat.Extensions
{
    public static class IChatContextExtensions
    {
        public static uint CalculateLength(this IChatContext context, string username, string message)
        {
            ChatMessage chatMessage = new(username, message);

            return context.CalculateLength(chatMessage);
        }

        public static void Insert(this IChatContext context, int index, string username, string message, string? externalId = null)
        {
            ChatMessage chatMessage = new(username, message, externalId);

            context.Insert(index, chatMessage);
        }

        public static void SendMessage(this IChatContext context, string username, string message, string? externalId = null)
        {
            ChatMessage chatMessage = new(username, message, externalId);

            context.SendMessage(chatMessage);
        }
    }
}