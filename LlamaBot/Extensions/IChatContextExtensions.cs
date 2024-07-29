using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Tokens.Models;

namespace LlamaBot.Extensions
{
    public static class IChatContextExtensions
    {
        public static void Insert(this IChatContext context, int index, TokenMask contentMask, string username, string message, ulong externalId)
        {
            ChatMessage chatMessage = new(contentMask, username, message, $"{externalId}");

            context.Insert(index, chatMessage);
        }
    }
}