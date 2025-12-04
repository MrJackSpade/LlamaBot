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

            List<int[]> sequenceTokenCollection = [];

            foreach (string s in _settings.SubSequences)
            {
                int[] thisSequence = NativeApi.Tokenize(context.ModelHandle, s, false);
                sequenceTokenCollection.Add(thisSequence);
            }

            int[]? currentSequence = sequenceTokenCollection.FirstOrDefault(context.ContextTokens.EndsWith);

            if (currentSequence is null)
            {
                return;
            }

            HashSet<int> banTokens = [];

            int start = currentSequence.Length;

            //-1 because otherwise we align with the end of the sequence
            long end = context.ContextTokens.Count - currentSequence.Length - 1;

            for (int i = (int)end; i > start; i--)
            {
                bool anyMatch = false;

                foreach (int[] checkSequence in sequenceTokenCollection)
                {
                    bool match = true;

                    for (int j = 0; j < currentSequence.Length; j++)
                    {
                        if (currentSequence[j] != context.ContextTokens[i + j].Id)
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        anyMatch = true;
                        break;
                    }
                }

                if (anyMatch)
                {
                    int nextToken = context.ContextTokens[i + currentSequence.Length].Id;

                    if (_settings.Exclude.Contains(nextToken))
                    {
                        continue;
                    }

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