using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Interfaces;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Models;
using LlamaNative.Serialization;

namespace LlamaNative.Chat
{
    public static class LlamaChatClient
    {
        public static IChatContext LoadChatContext(ChatSettings settings)
        {
            ITokenSelector tokenSelector = SamplerDeserializer.InstantiateSelector(settings.TokenSelector);

            List<ISimpleSampler> simpleSamplers = [];

            foreach (SamplerSetting samplerSetting in settings.SimpleSamplers)
            {
                simpleSamplers.Add(SamplerDeserializer.InstantiateSimple(samplerSetting));
            }

            INativeContext context = LlamaClient.LoadContext(settings.ModelSettings,
                                                             settings.ContextSettings,
                                                             tokenSelector,
                                                             [.. simpleSamplers]);

            return new ChatContext(settings, context);
        }
    }
}