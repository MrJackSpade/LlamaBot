using LlamaNative.Apis;
using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Interfaces;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Models;
using LlamaNative.Serialization;

namespace LlamaNative.Chat
{
    public static class LlamaChatClient
    {
        public static IChatContext LoadChatContext(ChatSettings settings)
        {
            Model model = LlamaClient.LoadModel(settings.ModelSettings);

            if(settings.SamplerSets.Count == 0)
            {
                throw new ArgumentException("Samplers and logit bias must be migrated to SamplerSets");
            }

            List<SamplerSet> samplerSets = new();

            int v = NativeApi.NVocab(model.Handle);

            string[] tokenValues = new string[v];

            for (int i = 0; i < v; i++)
            {
                try
                {
                    string token = NativeApi.TokenToPiece(model.Handle, i);

                    tokenValues[i] = token;
                }
                catch (Exception e)
                {
                }
            }

            foreach (SamplerSetConfiguration samplerSet in settings.SamplerSets)
            {
                SamplerSet newSet = new() { 
                    TokenSelector = SamplerDeserializer.InstantiateSelector(samplerSet.TokenSelector),
                    LogitBias = samplerSet.LogitBias
                };

                for (int i = 0; i < v; i++)
                {
                    string token = tokenValues[i];

                    if(token is null)
                    {
                        continue;
                    }

                    foreach (KeyValuePair<char, string> charBias in samplerSet.CharBias)
                    {
                        if (token.Contains(charBias.Key))
                        {
                            newSet.LogitBias.Add(i, charBias.Value);
                        }
                    }
                }

                foreach (SamplerSetting samplerSetting in samplerSet.SimpleSamplers)
                {
                    newSet.TypedSimpleSamplers.Add(SamplerDeserializer.InstantiateSimple(samplerSetting));
                }

                if(samplerSet.Push is not null && samplerSet.Pop is not null)
                {
                    int[] pushTokens = NativeApi.Tokenize(model.Handle, samplerSet.Push, false);

                    if(pushTokens.Length > 1)
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

            INativeContext context = LlamaClient.LoadContext(model,
                                                             settings.ContextSettings,
                                                             samplerSets);

            return new ChatContext(settings, context);
        }
    }
}