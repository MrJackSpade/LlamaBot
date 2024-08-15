using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Tokens.Models;

namespace LlamaBot.Extensions
{
    public static class IChatContextExtensions
    {
        public static void SendMessage(this IChatContext chatContext, CharacterMessage message, bool isBot)
        {
            chatContext.SendMessage(new ChatMessage(
                    isBot ? TokenMask.Bot : TokenMask.User,
                    message.User,
                    message.Content
                    ));
        }
    }
}