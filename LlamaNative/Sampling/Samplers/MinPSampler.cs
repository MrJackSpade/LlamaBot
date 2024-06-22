using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Extensions;

namespace LlamaNative.Sampling.Samplers
{
    public class MinPSampler : ISimpleSampler
    {
        private readonly MinPSamplerSettings _settings;

        public MinPSampler(MinPSamplerSettings temperatureSamplerSettings)
        {
            _settings = temperatureSamplerSettings;
        }

        public void ApplyOriginalMinP(SampleContext context)
        {
            Dictionary<int, int> mapping = new();

            Span<TokenData> newData = context.Candidates.Data.Span;

            for (int i = 0; i < context.Candidates.Data.Length; i++)
            {
                TokenData newToken = newData[i];
                mapping.Add(newToken.id, i);
            }

            foreach (TokenData token in context.OriginalCandidates)
            {
                float minp = _settings.MinP;

                if (_settings.MinPs.TryGetValue(token.id, out float cminp))
                {
                    minp = Math.Max(minp, cminp);
                }

                if (token.p < minp)
                {
                    int newIndex = mapping[token.id];
                    context.Candidates.SetLogitAtIndex(newIndex, float.NegativeInfinity);
                }
            }
        }

        public void SampleNext(SampleContext sampleContext)
        {
            SamplingApi.SoftMax(sampleContext.Candidates);
            ApplyOriginalMinP(sampleContext);
            SamplingApi.SoftMax(sampleContext.Candidates);

            SamplingApi.MinP(sampleContext.Candidates, _settings.MinP);
        }
    }
}