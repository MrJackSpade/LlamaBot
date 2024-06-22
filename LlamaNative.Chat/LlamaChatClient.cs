using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Interfaces;

namespace LlamaNative.Chat
{
    public static class LlamaChatClient
    {

        public static IChatContext LoadChatContext(ChatSettings settings)
        {
            INativeContext context = LlamaClient.LoadContext(settings.ModelSettings,
                                                             settings.ContextSettings,
                                                             settings.TokenSelector,
                                                             settings.SimpleSamplers.ToArray());

            return new ChatContext(settings, context);
        }
    }
}
