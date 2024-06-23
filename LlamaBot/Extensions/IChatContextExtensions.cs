using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;

namespace LlamaBot.Extensions
{
    public static class IChatContextExtensions
    {
        public static void Insert(this IChatContext context, int index, string username, string message, ulong externalId)
        {
            ChatMessage chatMessage = new(username, message, $"{externalId}");

            context.Insert(index, chatMessage);
        }
    }
}
