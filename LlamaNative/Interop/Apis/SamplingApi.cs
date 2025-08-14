using LlamaNative.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Extensions;
using LlamaNative.Tokens.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LlamaNative.Interop.Apis
{
    public unsafe class SamplingApi
    {
        private static readonly ConcurrentDictionary<nint, ConcurrentDictionary<int, string>> _tokenToPieceCache = new();

        public static void MinP(TokenDataArray candidates, float min)
        {
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                TokenData data = candidates.Data.Span[i];

                if (data.P < min)
                {
                    candidates.Data.Span[i].Logit = float.NegativeInfinity;
                }
            }

            candidates.Ordered = false;
        }

        public static void MinP(TokenDataArray candidates, int tokenId, float min)
        {
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                TokenData data = candidates.Data.Span[i];

                if (data.Id == tokenId)
                {
                    if (data.P < min)
                    {
                        candidates.Data.Span[i].Logit = float.NegativeInfinity;
                        candidates.Ordered = false;
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Frequency and presence penalties described in OpenAI API https://platform.openai.com/docs/api-reference/parameter-details.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="check_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="alpha_frequency"></param>
        /// <param name="alpha_presence"></param>
        public static void RepetitionPenalties(TokenDataArray candidates, int[] lastTokens, float penaltyRepeat, float penaltyFreq, float penaltyPresent, float slopeRepeat = 0)
        {
            // Early return condition
            if (lastTokens.Length == 0 || (penaltyRepeat == 1.0f && penaltyFreq == 0.0f && penaltyPresent == 0.0f))
            {
                return;
            }

            // Create a frequency map
            Dictionary<int, FoundTokenData> tokenCount = [];

            for (int i = 0; i < lastTokens.Length; i++)
            {
                if (!tokenCount.TryGetValue(lastTokens[i], out FoundTokenData ftd))
                {
                    ftd = new FoundTokenData();
                    tokenCount[lastTokens[i]] = ftd;
                }

                ftd.Count++;
                ftd.LastIndex = i;
            }

            // Apply penalties
            for (int i = 0; i < (int)candidates.Size; i++)
            {
                if (!tokenCount.TryGetValue(candidates.Data.Span[i].Id, out FoundTokenData ftd))
                {
                    continue;
                }

                if (penaltyRepeat > 0)
                {
                    float adjPenalty = CalculateAdjustedPenalty(penaltyRepeat, slopeRepeat, ftd.LastIndex, lastTokens.Length);

                    // Applying penalties
                    if (candidates.Data.Span[i].Logit <= 0)
                    {
                        candidates.Data.Span[i].Logit *= adjPenalty;
                    }
                    else
                    {
                        candidates.Data.Span[i].Logit /= adjPenalty;
                    }
                }

                float penalty = (ftd.Count * penaltyFreq) + ((ftd.Count > 0 ? 1f : 0f) * penaltyPresent);

                candidates.Data.Span[i].Logit -= penalty;
            }

            candidates.Ordered = false;
        }

        /// <summary>
        /// Sorts candidate tokens by their logits in descending order and calculate probabilities based on logits.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        public static void SoftMax(TokenDataArray candidates, bool sort)
        {
            if (candidates.Size <= 0)
            {
                throw new InvalidOperationException("Candidates array cannot be empty.");
            }

            Span<TokenData> candidateSpan = candidates.Data.Span;

            if (!candidates.Ordered && sort)
            {
                MemoryExtensions.Sort(candidateSpan, (a, b) => b.Logit.CompareTo(a.Logit));
                candidates.Ordered = true;
            }
            else if (candidates.Calculated)
            {
                return;
            }

            float maxLogit = 0;

            if (candidates.Ordered)
            {
                maxLogit = candidateSpan[0].Logit;
            }
            else
            {
                for (int ci = 0; ci < candidateSpan.Length; ci++)
                {
                    maxLogit = Math.Max(candidateSpan[ci].Logit, maxLogit);
                }
            }

            float cumSum = 0.0f;

            // Single pass for exp and sum
            for (int i = 0; i < candidateSpan.Length; i++)
            {
                float expValue = MathF.Exp(candidateSpan[i].Logit - maxLogit);
                candidateSpan[i].P = expValue;
                cumSum += expValue;
            }

            // Normalize using multiplication
            float invSum = 1.0f / cumSum;
            for (int i = 0; i < candidateSpan.Length; i++)
            {
                candidateSpan[i].P *= invSum;
            }

            candidates.Calculated = true;
        }

        /// <summary>
        /// Tail Free Sampling described in https://www.trentonbricken.com/Tail-Free-Sampling/.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="z"></param>
        /// <param name="min_keep"></param>
        public static void TailFree(TokenDataArray candidates, float z, int minKeep)
        {
            if (z >= 1.0f || candidates.Size <= 2)
            {
                return;
            }

            SoftMax(candidates, true);

            List<float> firstDerivatives = [];
            for (int i = 0; i < (int)(candidates.Size - 1); i++)
            {
                firstDerivatives.Add(candidates.Data.Span[i].P - candidates.Data.Span[i + 1].P);
            }

            List<float> secondDerivatives = [];
            for (int i = 0; i < firstDerivatives.Count - 1; i++)
            {
                secondDerivatives.Add(firstDerivatives[i] - firstDerivatives[i + 1]);
            }

            for (int i = 0; i < secondDerivatives.Count; i++)
            {
                secondDerivatives[i] = Math.Abs(secondDerivatives[i]);
            }

            NormalizeSecondDerivatives(secondDerivatives);

            float cumSum = 0.0f;
            int lastIdx = (int)candidates.Size;
            for (int i = 0; i < secondDerivatives.Count; i++)
            {
                cumSum += secondDerivatives[i];
                if (cumSum > z && i >= minKeep)
                {
                    lastIdx = i + 1; // Adjusted to C# indexing
                    break;
                }
            }

            candidates.Size = (ulong)lastIdx;

            for (int i = lastIdx + 1; i < candidates.Data.Span.Length; i++)
            {
                candidates.Data.Span[i].Logit = float.NegativeInfinity;
            }

            candidates.Ordered = false;
        }

        public static void Temperature(TokenDataArray candidates, float temp)
        {
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                candidates.Data.Span[i].Logit = candidates.Data.Span[i].Logit / temp;
            }

            candidates.Ordered = false;
        }

        public static void Temperature(TokenDataArray candidates, int index, float temp)
        {
            candidates.Data.Span[index].Logit = candidates.Data.Span[index].Logit / temp;

            candidates.Ordered = false;
        }

        /// <summary>
        /// Randomly selects a token from the candidates based on their probabilities.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <returns></returns>
        public static int Token(TokenDataArray candidates)
        {
            SoftMax(candidates, false);

            Random random = Random.Shared;
            double sum = 0;
            double r = random.NextDouble();

            float lastHighest = int.MaxValue;

            TokenData thisHighest;

            while (true)
            {
                thisHighest = candidates[0];

                for (uint i = 1; i < candidates.Size; i++)
                {
                    TokenData check = candidates[i];

                    if(check.Logit < lastHighest && check.Logit > thisHighest.Logit)
                    {
                        thisHighest = check;
                    }
                }

                sum += thisHighest.P;

                if(sum >= r)
                {
                    return thisHighest.Id;
                }

                lastHighest = thisHighest.Logit;
            }
        }

        /// <summary>
        /// Selects the token with the highest probability.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <returns></returns>
        public static int TokenGreedy(TokenDataArray candidates)
        {
            return candidates.GetMostLikely().Id;
        }

        /// <summary>
        /// Mirostat 1.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `TokenData` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="m">The number of tokens considered in the estimation of `s_hat`. This is an arbitrary value that is used to calculate `s_hat`, which in turn helps to calculate the value of `k`. In the paper, they use `m = 100`, but you can experiment with different values to see how it affects the performance of the algorithm.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        public static int TokenMirostat(SafeContextHandle ctx, TokenDataArray candidates, float tau, float eta, int m, ref float mu)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            TokenDataArrayNative st = new()
            {
                data = new nint(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Ordered
            };
            int res;
            fixed (float* pmu = &mu)
            {
                res = LlamaCppApi.SampleTokenMirostat(ctx, new nint(&st), tau, eta, m, pmu);
            }

            return res;
        }

        /// <summary>
        /// Mirostat 2.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `TokenData` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        public static int TokenMirostatV2(SafeContextHandle ctx, TokenDataArray candidates, float tau, float eta, ref float mu)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            TokenDataArrayNative st = new()
            {
                data = new nint(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Ordered
            };
            int res;
            fixed (float* pmu = &mu)
            {
                res = LlamaCppApi.SampleTokenMirostatV2(ctx, new nint(&st), tau, eta, pmu);
            }

            return res;
        }

        /// <summary>
        /// Top-K sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="k"></param>
        /// <param name="min_keep"></param>
        public static void TopK(TokenDataArray candidates, int k, int min_keep)
        {
            SoftMax(candidates, true);

            for (int i = Math.Max(k, min_keep); i < candidates.Data.Span.Length; i++)
            {
                candidates.Data.Span[i].Logit = float.NegativeInfinity;
            }

            candidates.Size = (ulong)Math.Max(k, min_keep);
        }

        /// <summary>
        /// Nucleus sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        public static void TopP(SafeContextHandle ctx, TokenDataArray candidates, float p, ulong min_keep)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            TokenDataArrayNative st = new()
            {
                data = new nint(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Ordered
            };

            LlamaCppApi.SampleTopP(ctx, new nint(&st), p, min_keep);

            candidates.Size = st.size;
            candidates.Ordered = st.sorted;
        }

        public static bool TryTokenToPiece(SafeModelHandle handle, int tokenId, out string? result)
        {
            if (!_tokenToPieceCache.TryGetValue(handle.Handle, out ConcurrentDictionary<int, string>? modelTokens))
            {
                modelTokens = new ConcurrentDictionary<int, string>();
                _tokenToPieceCache[handle.Handle] = modelTokens;
            }

            if (!modelTokens.TryGetValue(tokenId, out result))
            {
                try
                {
                    result = handle.TokenToPiece(tokenId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                modelTokens.TryAdd(tokenId, result);
            }

            return result != null;
        }

        /// <summary>
        /// Locally Typical Sampling implementation described in the paper https://arxiv.org/abs/2202.00666.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        public static void Typical(SafeContextHandle ctx, TokenDataArray candidates, float p, ulong min_keep)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            TokenDataArrayNative st = new()
            {
                data = new nint(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Ordered
            };

            LlamaCppApi.SampleTypical(ctx, new nint(&st), p, min_keep);

            candidates.Size = st.size;
            candidates.Ordered = st.sorted;
        }

        // Assumptions:
        // ftd.LastIndex is a value that decreases as we get closer to the token we want to apply the penalty to.
        // lastTokens.Length is the total number of tokens we're considering.
        // penaltyRepeat is the original penalty value.
        // slope controls the rate of change of the penalty.
        private static float CalculateAdjustedPenalty(float penaltyRepeat, float slope, int ftdLastIndex, int lastTokensLength)
        {
            if (slope == 0)
            {
                // When slope is 0, penaltyRepeat remains unchanged
                return penaltyRepeat;
            }
            else
            {
                // Calculate the normalized position of ftd.LastIndex in the range [0, lastTokens.Length]
                float normalizedIndex = (float)ftdLastIndex / lastTokensLength;

                // Adjust the penaltyRepeat to approach 1 as ftd.LastIndex approaches 0
                // This creates a linear interpolation between penaltyRepeat and 1, controlled by slope
                float adjustedPenalty = ((1 - normalizedIndex) * slope * (1 - penaltyRepeat)) + penaltyRepeat;

                return adjustedPenalty;
            }
        }

        private static void NormalizeSecondDerivatives(List<float> secondDerivatives)
        {
            float sum = secondDerivatives.Sum();
            if (sum > 1e-6f)
            {
                for (int i = 0; i < secondDerivatives.Count; i++)
                {
                    secondDerivatives[i] /= sum;
                }
            }
            else
            {
                float equalValue = 1.0f / secondDerivatives.Count;
                for (int i = 0; i < secondDerivatives.Count; i++)
                {
                    secondDerivatives[i] = equalValue;
                }
            }
        }

        private class FoundTokenData
        {
            public int Count { get; set; } = 0;

            public int LastIndex { get; set; } = 0;
        }
    }
}