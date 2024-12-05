using LlamaNative.Apis;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Interfaces;
using LlamaNative.Models;
using LlamaNative.Sampling.Models;
using LlamaNative.Serialization;

namespace LlamaNative.Chat
{
    public static class LlamaChatClient
    {
        public static IChatContext LoadChatContext(ChatSettings settings)
        {
            Model model = LlamaClient.LoadModel(settings.ModelSettings);

            Model? draftModel = null;

            if (settings.DraftModelSettings is not null)
            {
                draftModel = LlamaClient.LoadModel(settings.DraftModelSettings);
            }

            if (settings.SamplerSets.Count == 0)
            {
                throw new ArgumentException("Samplers and logit bias must be migrated to SamplerSets");
            }

            List<SamplerSet> samplerSets = [];

            foreach (SamplerSetConfiguration samplerSet in settings.SamplerSets)
            {
                SamplerSet newSet = new()
                {
                    TokenSelector = SamplerDeserializer.InstantiateSelector(samplerSet.TokenSelector),
                    LogitBias = samplerSet.LogitBias
                };

                foreach (SamplerSetting samplerSetting in samplerSet.SimpleSamplers)
                {
                    newSet.SimpleSamplers.Add(SamplerDeserializer.InstantiateSimple(samplerSetting));
                }

                if (samplerSet.Push is not null && samplerSet.Pop is not null)
                {
                    int[] pushTokens = NativeApi.Tokenize(model.Handle, samplerSet.Push, false);

                    if (pushTokens.Length > 1)
                    {
                        throw new InvalidOperationException("Push tokens must be a single token");
                    }

                    newSet.Push = pushTokens[0];

                    int[] popTokens = NativeApi.Tokenize(model.Handle, samplerSet.Pop, false);

                    if (popTokens.Length > 1)
                    {
                        throw new InvalidOperationException("Pop tokens must be a single token");
                    }

                    newSet.Pop = popTokens[0];
                }

                samplerSets.Add(newSet);
            }

            INativeContext context;

            if (draftModel is null)
            {
                context = LlamaClient.LoadContext(model,
                                                  settings.ContextSettings,
                                                  samplerSets);
            }
            else
            {
                context = LlamaClient.LoadContext(model,
                                                  draftModel,
                                                  settings.ContextSettings,
                                                  samplerSets);
            }

            return new ChatContext(settings, context);
        }
    }
}