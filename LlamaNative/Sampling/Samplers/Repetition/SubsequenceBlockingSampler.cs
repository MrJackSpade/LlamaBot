using LlamaNative.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Extensions;

namespace LlamaNative.Sampling.Samplers.Repetition
{
    public class SubsequenceBlockingSampler(SubsequenceBlockingSamplerSettings settings) : ISimpleSampler
    {
        private readonly SubsequenceBlockingSamplerSettings _settings = settings;

        public void SampleNext(SampleContext context)
        {
            if (_settings.ResponseStartBlock == 0)
            {
                return;
            }

            int[] sequenceTokens = NativeApi.Tokenize(context.ModelHandle, _settings.SubSequence, false);

            bool inSequence = context.ContextTokens.EndsWith(sequenceTokens);

            if (!inSequence)
            {
                return;
            }

            HashSet<int> banTokens = [];

            int start = sequenceTokens.Length;

            //-1 because otherwise we align with the end of the sequence
            long end = context.ContextTokens.Count - sequenceTokens.Length - 1;

            for (int i = (int)end; i > start; i--)
            {
                bool match = true;

                for (int j = 0; j < sequenceTokens.Length; j++)
                {
                    if (sequenceTokens[j] != context.ContextTokens[i + j].Id)
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    int nextToken = context.ContextTokens[i + sequenceTokens.Length].Id;
                    banTokens.Add(nextToken);

                    if (banTokens.Count > _settings.ResponseStartBlock)
                    {
                        break;
                    }
                }
            }

            foreach (int banToken in banTokens)
            {
                context.Candidates.SetLogit(banToken, float.NegativeInfinity);
            }
        }
    }
}