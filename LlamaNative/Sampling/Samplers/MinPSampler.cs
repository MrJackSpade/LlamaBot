using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Interfaces;

namespace LlamaNative.Sampling.Samplers
{
    public class MinPSampler(MinPSamplerSettings temperatureSamplerSettings) : ISimpleSampler
    {
        private readonly MinPSamplerSettings _settings = temperatureSamplerSettings;

        public void ApplyOriginalMinP(SampleContext context)
        {
            bool[] trimIds = new bool[context.OriginalCandidates.Size];

            //The highest prob token
            TokenData dontTrim = context.OriginalCandidates[0];

            //Figure out what from the original set, falls below the min
            foreach (TokenData token in context.OriginalCandidates)
            {
                if (dontTrim.Logit < token.Logit)
                {
                    dontTrim = token;
                }

                float minP = this.GetMinP(token.Id);

                if (token.P < minP)
                {
                    trimIds[token.Id] = true;
                }
            }

            trimIds[dontTrim.Id] = false;

            //Actually trim
            Span<TokenData> span = context.Candidates.Data.Span;

            int s = 0;
            int e = 0;

            do
            {
                TokenData token = span[e];

                if (!trimIds[token.Id])
                {
                    if (s != e)
                    {
                        span[s] = span[e];
                    }

                    s++;
                }

                e++;
            } while (e < span.Length);

            context.Candidates.Size = (ulong)s;

            for (int i = 0; i < trimIds.Length; i++)
            {
                if (trimIds[i])
                {
                    span[s] = new TokenData()
                    {
                        Id = i,
                        Logit = float.NegativeInfinity
                    };

                    s++;
                }
            }
        }

        public void SampleNext(SampleContext sampleContext)
        {
            SamplingApi.SoftMax(sampleContext.Candidates);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates);
            this.ApplyOriginalMinP(sampleContext);
        }

        private float GetMinP(int id)
        {
            float minP = _settings.MinP;

            if (_settings.MinPs.TryGetValue(id, out float cminp))
            {
                minP = Math.Max(minP, cminp);
            }

            return minP;
        }
    }
}