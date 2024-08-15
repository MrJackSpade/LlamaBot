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

            //Skip the dictionary if the tokens are still in their original order
            if (context.Candidates.Sorted)
            {
                for (int i = 0; i < context.Candidates.Data.Length; i++)
                {
                    TokenData newToken = newData[i];
                    mapping.Add(newToken.Id, i);
                }
            }

            foreach (TokenData token in context.OriginalCandidates)
            {
                float minP = _settings.MinP;

                if (_settings.MinPs.TryGetValue(token.Id, out float cminp))
                {
                    minP = Math.Max(minP, cminp);
                }

                if (token.P < minP)
                {
                    int newIndex = context.Candidates.Sorted ? mapping[token.Id] : token.Id;
                    context.Candidates.SetLogitAtIndex(newIndex, float.NegativeInfinity);
                }
            }
        }

        public void SampleNext(SampleContext sampleContext)
        {
            SamplingApi.SoftMax(sampleContext.Candidates);
            this.ApplyOriginalMinP(sampleContext);
            SamplingApi.SoftMax(sampleContext.Candidates);

            SamplingApi.MinP(sampleContext.Candidates, _settings.MinP);
        }
    }
}