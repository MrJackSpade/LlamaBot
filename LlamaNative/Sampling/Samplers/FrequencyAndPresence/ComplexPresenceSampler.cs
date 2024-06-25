using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Sampling.Samplers.FrequencyAndPresence
{
    public class ComplexPresenceSampler(ComplexPresencePenaltySettings settings) : ISimpleSampler
    {
        private readonly ComplexPresencePenaltySettings _settings = settings;

        public void SampleNext(SampleContext sampleContext)
        {
            TokenCollection sampleTokens = sampleContext.ContextTokens.Trim();

            int[] lastTokens = this.GetLastTokens(sampleTokens, _settings.RepeatTokenPenaltyWindow).Ids;

            TokenDataArray candidates = sampleContext.Candidates;

            int minGroupLength = _settings.MinGroupLength;
            float scalePerGroup = _settings.GroupScale;
            float scalePerLength = _settings.LengthScale;

            if (minGroupLength == 0)
            {
                return;
            }

            int[] test_array = new int[lastTokens.Length + 1];

            for (int i = 0; i < lastTokens.Length; i++)
            {
                test_array[i] = lastTokens[i];
            }

            HashSet<int> candidate_ids = new(test_array);

            int num_threads = Environment.ProcessorCount;

            Range[] ranges = GetRanges(num_threads, candidates.Data.Length).ToArray();

            Parallel.ForEach(ranges, range => ProcessCandidates(candidates, test_array, minGroupLength, candidate_ids, range.Start.Value, range.End.Value, scalePerGroup, scalePerLength));

            candidates.Sorted = false;
        }

        private static IEnumerable<Range> GetRanges(int chunks, int total)
        {
            int l = total / chunks;
            int r = total % chunks;

            for (int i = 0; i < chunks; i++)
            {
                int s = i * l;
                int e = i * l + l;

                if (i < chunks - 1)
                {
                    yield return new Range(s, e);
                }
                else
                {
                    yield return new Range(s, e + r);
                }
            }
        }

        private static void ProcessCandidates(TokenDataArray candidates, int[] test_array, int minGroupLength, HashSet<int> candidate_ids, int start, int end, float scalePerGroup, float scalePerLength)
        {
            Span<TokenData> candidateSpan = candidates.Data.Span;

            int p_test = test_array.Length - 1;

            for (int i = start; i < end; i++)
            {
                int llama_token = candidateSpan[i].Id;

                if (!candidate_ids.Contains(llama_token))
                {
                    continue;
                }

                test_array[p_test] = llama_token;

                int n_group = 0;
                int n_len = 0;

                for (int p_dynamic = test_array.Length - 2; p_dynamic >= 0; p_dynamic--)
                {
                    int p2 = 0;

                    while (p_dynamic - p2 >= 0 && test_array[p_dynamic - p2] == test_array[p_test - p2])
                    {
                        p2++;
                    }

                    if (p2 >= minGroupLength)
                    {
                        n_group++;
                        n_len += p2;
                    }
                }

                if (n_group != 0)
                {
                    float g_penalty = (float)Math.Pow(scalePerGroup, n_group);
                    float l_penalty = (float)Math.Pow(scalePerLength, n_len);

                    if (candidateSpan[i].Logit <= 0)
                    {
                        candidateSpan[i].Logit *= g_penalty;
                        candidateSpan[i].Logit *= l_penalty;
                    }
                    else
                    {
                        candidateSpan[i].Logit /= g_penalty;
                        candidateSpan[i].Logit /= l_penalty;
                    }
                }
            }
        }
    }
}