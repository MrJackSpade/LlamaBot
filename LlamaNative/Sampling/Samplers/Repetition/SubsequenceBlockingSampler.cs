using LlamaNative.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Extensions;

namespace LlamaNative.Sampling.Samplers.Repetition
{
    public class SubsequenceBlockingSampler(SubsequenceBlockingSamplerSettings settings) : ISimpleSampler
    {
        private readonly SubsequenceBlockingSamplerSettings _settings = settings;

        public void SampleNext(SampleContext sampleContext)
        {
            if (_settings.ResponseStartBlock == 0)
            {
                return;
            }

            TokenCollection sequence = new TokenCollection(sampleContext.KvCache.GetSequence(0)).Trim();

            int[] sequenceTokens = NativeApi.Tokenize(sampleContext.ModelHandle, _settings.SubSequence, false);

            bool inSequence = sequence.EndsWith(sequenceTokens);

            if (!inSequence)
            {
                return;
            }

            HashSet<int> banTokens = [];

            int start = sequenceTokens.Length;

            //-1 because otherwise we align with the end of the sequence
            long end = sequence.Count - sequenceTokens.Length - 1;

            for (int i = (int)end; i > start; i--)
            {
                bool match = true;

                for (int j = 0; j < sequenceTokens.Length; j++)
                {
                    if (sequenceTokens[j] != sequence[i + j].Id)
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    int nextToken = sequence[i + sequenceTokens.Length].Id;
                    banTokens.Add(nextToken);

                    if (banTokens.Count > _settings.ResponseStartBlock)
                    {
                        break;
                    }
                }
            }

            foreach (int banToken in banTokens)
            {
                sampleContext.Candidates.SetLogit(banToken, float.NegativeInfinity);
                sampleContext.OriginalCandidates.SetLogit(banToken, float.NegativeInfinity);
            }
        }
    }
}