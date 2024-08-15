using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Extensions;

namespace LlamaNative.Sampling.Samplers
{
    public class MinPSampler(MinPSamplerSettings temperatureSamplerSettings) : ISimpleSampler
    {
        private readonly MinPSamplerSettings _settings = temperatureSamplerSettings;

        public void ApplyOriginalMinP(SampleContext context)
        {
            Dictionary<int, int> mapping = [];

            Span<TokenData> newData = context.Candidates.Data.Span;

            int highestId = -1;
            float highestLogit = float.NegativeInfinity;

            for (int i = 0; i < context.Candidates.Data.Length; i++)
            {
                TokenData newToken = newData[i];
                mapping.Add(newToken.Id, i);

                if(newToken.Logit > highestLogit)
                {
                    highestLogit = newToken.Logit;
                    highestId = newToken.Id;
                }
            }

            foreach (TokenData token in context.OriginalCandidates)
            {
                //Never sample away the highest probability token
                //Or shit will break
                if(token.Id == highestId)
                {
                    continue;
                }

                float minP = _settings.MinP;

                if (_settings.MinPs.TryGetValue(token.Id, out float cminp))
                {
                    minP = Math.Max(minP, cminp);
                }

                if (token.P < minP)
                {
                    int newIndex = mapping[token.Id];
                    context.Candidates.SetLogitAtIndex(newIndex, float.NegativeInfinity);
                }
            }
        }

        public void SampleNext(SampleContext sampleContext)
        {
            SamplingApi.SoftMax(sampleContext.Candidates);
            this.ApplyOriginalMinP(sampleContext);
        }
    }
}