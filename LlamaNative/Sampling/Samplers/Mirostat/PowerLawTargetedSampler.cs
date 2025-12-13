using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Extensions;
using LlamaNative.Tokens.Models;
using System.Diagnostics;

namespace LlamaNative.Sampling.Samplers.Mirostat
{
    /// <summary>
    /// A sampler that dynamically adjusts probabilities to target a specific "surprise" (entropy) level,
    /// similar to Mirostat but using a Power Law distribution to reshape the candidates.
    /// <para>
    /// <b>Why this exists:</b> <br/>
    /// Standard sampling (Top-P, Top-K) is static; it doesn't adapt to whether the model is confident or confused.
    /// This sampler uses a negative feedback loop:
    /// <list type="bullet">
    /// <item>If recent tokens were very probable (boring), it targets lower probability tokens to add "creativity".</item>
    /// <item>If recent tokens were improbable (chaotic), it targets higher probability tokens to restore coherence.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class PowerLawTargetedSampler : BaseDynamicSampler<PowerLawTargetedSamplerSettings>, ITokenSelector
    {
        public PowerLawTargetedSampler(PowerLawTargetedSamplerSettings settings) : base(settings.QueueSize, settings)
        {
            // Pre-process greedy exclude IDs for faster lookup
            foreach (int id in _settings.GreedyExclude)
            {
                _isWords.Add(id, true);
            }
        }

        /// <summary>
        /// Calculates the target probability for the next token based on recent history (Negative Feedback Loop).
        /// </summary>
        /// <returns>The target probability (0.0 to 1.0) for the next token.</returns>
        public float CalculateNextTarget()
        {
            return this.ComputeTarget(_settings.MinTarget, _settings.MaxTarget, _settings.TailDecay);
        }

        public int SampleNext(SampleContext sampleContext)
        {
            // 1. Ensure candidates are sorted and have probabilities (SoftMax)
            SamplingApi.SoftMax(sampleContext.Candidates, false);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates, false);

            // Filter candidates based on MinP/TopK/etc settings from base class
            Span<TokenData> candidateSpan = sampleContext.Candidates.Data.Span;
            Span<TokenData> originalSpan = sampleContext.OriginalCandidates.Data.Span;

            List<TokenData> candidates = this.FilterCandidates(sampleContext);

            // 2. Determine our target probability
            float target = this.CalculateNextTarget();

            // 3. Check for "Top Only" bypass conditions.
            // If certain conditions are met, we skip the fancy sampling and just pick the winner (Greedy).
            // This is crucial for stability and correctness.
            bool topOnly = false;
            string topOnlyReason = "";

            TokenData topToken = sampleContext.OriginalCandidates.GetMostLikely();

            if (!_settings.GreedyExclude.Contains(topToken.Id))
            {
                // FORCE: Always pick this token if it's in the include list (e.g. forced endings)
                if (_settings.GreedyInclude.Contains(topToken.Id))
                {
                    topOnly = true;
                    topOnlyReason = "Greedy Include (Forced)";
                }
                // MAX-P: If a specific token exceeds its defined max probability, pick it immediately.
                // Useful for stopping generation when confidence is extremely high on a stop token.
                else if (_settings.MaxPs.TryGetValue(topToken.Id, out float maxP))
                {
                    if (topToken.P >= maxP)
                    {
                        topOnly = true;
                        topOnlyReason = $"Max Probability Exceeded ({topToken.P:F4} >= {maxP:F4})";
                    }
                }
                // PRESERVED WORDS: If the token completes a word (isn't a new word start) and is confident enough.
                // Why? Breaking a multi-token word (e.g. "import" -> "im" + "port") with a low-probability alternative
                // usually results in a typo or nonsense. We protect established words.
                else if (this.IsWordCompletion(sampleContext.ModelHandle, topToken.Id))
                {
                    if (topToken.P > _settings.PreserveWordMaxP)
                    {
                        topOnly = true;
                        topOnlyReason = $"Word Preservation (Confident: {topToken.P:F4} > {_settings.PreserveWordMaxP:F4})";
                    }
                }
            }

            int selectedToken;

            if (topOnly)
            {
                selectedToken = topToken.Id;
            }
            else
            {
                // Apply Power Law distribution to candidates based on proximity to target
                selectedToken = this.ApplyPowerLawDistribution(candidates, target, sampleContext);
            }

            if (_settings.Log)
            {
                this.LogPowerLawState(sampleContext, target, _settings.MinTarget, _settings.MaxTarget, _settings.TailDecay, selectedToken, topOnly, topOnlyReason);
            }
            else
            {
                Token token = sampleContext.GetToken(TokenMask.Undefined, selectedToken);

                Debug.WriteLine($"[{selectedToken}] \"{token.GetEscapedValue()}\"");
            }

            if (!topOnly || _settings.FactorPreservedWords)
            {
                TokenData originalP = sampleContext.GetOriginalData(selectedToken);
                this.Push(originalP);
            }

            return selectedToken;
        }

        /// <summary>
        /// Reshapes the probability distribution of candidates to prioritize those closest to the target probability.
        /// </summary>
        /// <param name="candidates">The filtered list of eligible candidates.</param>
        /// <param name="target">The target probability we want to hit.</param>
        /// <param name="context">Sampling context.</param>
        /// <returns>The ID of the selected token.</returns>
        private int ApplyPowerLawDistribution(List<TokenData> candidates, float target, SampleContext context)
        {
            // Create a work copy of candidates to modify
            Tokens.Models.TokenDataArray candidatesArray = context.Candidates;
            Span<TokenData> candidatesSpan = candidatesArray.Data.Span;

            candidatesArray.Ordered = false;

            // Reset logits of all tokens to a very low value (effectively removing them).
            // We will only "turn on" the tokens that survived the filter.
            for (int i = 0; i < candidatesSpan.Length; i++)
            {
                candidatesSpan[i].Logit = -100.0f; // Effectively -inf for tokens below threshold
            }

            // Find the closest token to target (for potentially special handling when width ~ 0)
            float minDistance = float.MaxValue;
            int closestTokenId = -1;

            // Track if any token is above min-p
            bool anyTokenAboveMinP = false;

            // Track max probability token for fallback case
            float maxP = float.MinValue;
            int maxPTokenId = -1;

            foreach (TokenData candidate in candidates)
            {
                // Track token with highest probability for fallback
                if (candidate.P > maxP)
                {
                    maxP = candidate.P;
                    maxPTokenId = candidate.Id;
                }

                // Check if any token is above min-p threshold
                if (candidate.P >= _settings.MinP)
                {
                    anyTokenAboveMinP = true;
                }

                float distance = Math.Abs(candidate.P - target);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTokenId = candidate.Id;
                }
            }

            // If no tokens are above min-p, directly return the token with max probability
            if (!anyTokenAboveMinP && maxPTokenId != -1)
            {
                return maxPTokenId;
            }

            // Apply Power Law distribution to valid candidates
            // This is the core "Magic" of this sampler.
            foreach (TokenData candidate in candidates)
            {
                float distance = Math.Abs(candidate.P - target);
                int tokenIdx = -1;

                // Find token index in the original array
                for (int j = 0; j < candidatesSpan.Length; j++)
                {
                    if (candidatesSpan[j].Id == candidate.Id)
                    {
                        tokenIdx = j;
                        break;
                    }
                }

                if (tokenIdx >= 0)
                {
                    if (candidate.P < _settings.MinP)
                    {
                        candidatesSpan[tokenIdx].Logit = -100.0f; // Effectively -inf for tokens below threshold
                        continue; // Skip candidates below the minimum probability
                    }

                    if (_settings.DistributionWidth <= float.Epsilon) // Handle case where width is effectively zero (Dirac delta)
                    {
                        candidatesSpan[tokenIdx].Logit = candidate.Id == closestTokenId ? _settings.PeakLogitValue : -100.0f;
                    }
                    else
                    {
                        // Reshape the logits using a Power Law-like distribution (Generalized Cauchy).
                        // 1. Calculate normalized distance from the target probability.
                        float normalizedDistance = distance / Math.Max(0.001f, _settings.DistributionWidth);

                        // 2. Apply formula: Peak / (1 + Distance^TailHeaviness)
                        // - Items CLOSE to the target get a HIGH logit (boosted).
                        // - Items FAR from the target get a LOW logit (suppressed).
                        // - TailHeaviness controls how quickly the boost drops off.
                        candidatesSpan[tokenIdx].Logit = (float)(_settings.PeakLogitValue / (1.0f + Math.Pow(normalizedDistance, _settings.TailHeaviness)));
                    }
                }
            }

            context.Candidates.Ordered = false;

            SamplingApi.SoftMax(context.Candidates, false);

            // Sample using temperature sampling
            int sampled = SamplingApi.Token(candidatesArray);

            return sampled;
        }

        // Computes the target probability for the current sampling step.
        // This function implements a dynamic feedback mechanism to maintain
        // an average selection probability close to the base target over time.
        private float ComputeTarget(float minTarget, float maxTarget, float tailDecay)
        {
            float computedTarget = _settings.Target;
            int sz = SelectionHistory.Count;

            if (sz > 0)
            {
                // Check if window is at capacity (oldest element will be evicted on next push)
                bool windowFull = sz == QueueSize;

                // Compute weighted sum with exponential decay
                // rat(0) = newest in buffer, gets weight 1
                // rat(i) gets weight decay^i
                //
                // When window is full: exclude oldest element (it will be evicted)
                // When window is not full: include all elements (nothing evicted)
                float weightedSum = 0.0f;
                float weight = 1.0f;
                int elementsToSum = windowFull ? (sz - 1) : sz;

                // Access items by index (newest is at end)
                TokenData[] history = SelectionHistory.ToArray();

                for (int i = 0; i < elementsToSum; ++i)
                {
                    // history[sz - 1] is newest (rat 0)
                    // history[sz - 1 - i] is rat(i)
                    weightedSum += history[sz - 1 - i].P * weight;
                    weight *= tailDecay;
                }

                // Compute total weight after new value is inserted
                // When full: sz elements remain (oldest evicted, new added)
                // When not full: sz + 1 elements (new added, nothing evicted)
                int finalElementCount = windowFull ? sz : (sz + 1);

                float totalWeight;
                if (Math.Abs(tailDecay - 1.0f) < float.Epsilon)
                {
                    totalWeight = (float)finalElementCount;
                }
                else
                {
                    totalWeight = (1.0f - (float)Math.Pow(tailDecay, finalElementCount)) / (1.0f - tailDecay);
                }

                // Shift weights to account for new value taking position 0
                // All existing values age by 1, so multiply their weights by decay
                float shiftedWeightedSum = weightedSum * tailDecay;

                // Solve for the new value that achieves target weighted average
                float nextValue = (_settings.Target * totalWeight) - shiftedWeightedSum;

                // Clamp to allowed range
                computedTarget = Math.Clamp(nextValue, minTarget, maxTarget);
            }

            return computedTarget;
        }

        private void LogPowerLawState(SampleContext ctx, float clampedTarget, float minTarget, float maxTarget, float tailDecay, int selectedIdx, bool topOnly, string topOnlyReason)
        {
            int sz = SelectionHistory.Count;
            bool windowFull = sz == QueueSize;
            float selectedProb = ctx.GetOriginalData(selectedIdx).P;
            int elementsToSum = windowFull ? (sz - 1) : sz;

            // Recompute weighted sum for logging
            float weightedSum = 0.0f;
            float weight = 1.0f;
            TokenData[] history = SelectionHistory.ToArray();

            for (int i = 0; i < elementsToSum; ++i)
            {
                weightedSum += history[sz - 1 - i].P * weight;
                weight *= tailDecay;
            }

            // Compute total weight after insertion
            int finalCount = windowFull ? sz : (sz + 1);
            float totalWeight;
            if (Math.Abs(tailDecay - 1.0f) < float.Epsilon)
            {
                totalWeight = (float)finalCount;
            }
            else
            {
                totalWeight = (1.0f - (float)Math.Pow(tailDecay, finalCount)) / (1.0f - tailDecay);
            }

            float shiftedSum = weightedSum * tailDecay;
            float rawTarget = (_settings.Target * totalWeight) - shiftedSum;

            Debug.WriteLine("========================");
            if (topOnly)
            {
                Debug.WriteLine($"PowerLawSampler: TOP ONLY BYPASS - {topOnlyReason}");
            }

            Debug.WriteLine($"PowerLawSampler: Final Target: computed={rawTarget:F4}, clamped={clampedTarget:F4}");

            // Top candidates
            int topN = (int)Math.Min(4ul, ctx.OriginalCandidates.Size);
            Debug.Write($"PowerLawSampler: Candidates (Top {topN}): ");

            // We need sorted original candidates for the "Top N" display
            // The OriginalCandidates might not be sorted if they weren't used that way,
            // but BaseDynamicSampler usually ensures they are accessible.
            // ctx.OriginalCandidates is a TokenDataArray.
            // SoftMax was called on it in SampleNext.
            // We need to copy/sort to be safe or use GetMostLikely which implies order...
            // Actually SampleNext calls SoftMax(OriginalCandidates, false) which sorts them!
            Span<TokenData> topCandidates = ctx.OriginalCandidates.Data.Span[..topN];

            for (int i = 0; i < topN; ++i)
            {
                Debug.Write($"{{id:{topCandidates[i].Id}, p:{topCandidates[i].P:F4}}}");
                if (i < topN - 1)
                {
                    Debug.Write(", ");
                }
            }

            Debug.WriteLine("");

            Debug.WriteLine($"PowerLawSampler: Selected: {{id:{selectedIdx}, p:{selectedProb:F4}}}");

            Debug.WriteLine("---------------------------");

            // Full history (showing what it will be after push)
            int historySz = windowFull ? sz : (sz + 1);
            Debug.Write($"PowerLawSampler: Full History ({historySz}): [");

            if (sz > 0)
            {
                int startIdx = windowFull ? (sz - 2) : (sz - 1);
                for (int i = 0; i <= startIdx; ++i)
                {
                    Debug.Write($"{history[sz - 1 - (startIdx - i)].P:F4}, ");
                }
            }

            Debug.WriteLine($"{selectedProb:F4}]");

            if (windowFull && sz > 0)
            {
                Debug.WriteLine($"PowerLawSampler: OLDEST (excluded): {history[0].P:F4}  <-- This will be evicted");
            }

            // Calc window (elements used, oldest to newest)
            Debug.Write($"PowerLawSampler: Calc Window ({elementsToSum}): [");
            if (elementsToSum > 0)
            {
                for (int i = 0; i < elementsToSum; ++i)
                {
                    // "elements_to_sum - 1 - i" in C++ moves from oldest to newest in the *summation* window?
                    // C++: ctx->window.rat(elements_to_sum - 1 - i)
                    // rat(0) is newest. rat(k) is older.
                    // So rat(elements_to_sum - 1) is the oldest included element.
                    // rat(0) is the newest included element.
                    // If i=0, we access rat(max_idx). If i=max, we access rat(0).
                    // So it is printing Oldest -> Newest.

                    // In our history array, history[sz-1] is newest (rat 0).
                    // history[sz-1-k] is rat(k).
                    // We want: rat(elements_to_sum - 1 - i).
                    // So index = sz - 1 - (elements_to_sum - 1 - i)
                    //          = sz - 1 - elements_to_sum + 1 + i
                    //          = sz - elements_to_sum + i

                    int index = sz - elementsToSum + i;
                    Debug.Write($"{history[index].P:F4}");
                    if (i < elementsToSum - 1)
                    {
                        Debug.Write(", ");
                    }
                }
            }

            Debug.WriteLine("]");

            // Stats with exponential decay
            Debug.WriteLine($"PowerLawSampler: Calc Window Stats (decay={tailDecay:F4}):");
            Debug.WriteLine($"    Weighted sum of {elementsToSum} values: {weightedSum:F4}");
            Debug.WriteLine($"    Total weight after insert: {totalWeight:F4}");
            Debug.WriteLine($"    Shifted weighted sum (sum * decay): {shiftedSum:F4}");

            Debug.WriteLine("PowerLawSampler: Target Calculation:");
            Debug.WriteLine("    Formula: (target * total_weight) - shifted_sum");
            Debug.WriteLine($"    ({_settings.Target:F4} * {totalWeight:F4}) - {shiftedSum:F4} = {rawTarget:F4}");

            // Verification
            Debug.WriteLine("PowerLawSampler: === VERIFICATION ===");
            float newWeightedSum = selectedProb + shiftedSum;
            float newWeightedAvg = newWeightedSum / totalWeight;
            Debug.WriteLine($"    New weighted sum: {selectedProb:F4} + {shiftedSum:F4} = {newWeightedSum:F4}");
            Debug.WriteLine($"    New weighted avg: {newWeightedSum:F4} / {totalWeight:F4} = {newWeightedAvg:F6}");
            Debug.WriteLine($"    Desired target: {_settings.Target:F6}");
            Debug.WriteLine($"    Difference: {Math.Abs(newWeightedAvg - _settings.Target):F8}");

            bool wasClamped = rawTarget < minTarget || rawTarget > maxTarget;
            bool matches = Math.Abs(newWeightedAvg - _settings.Target) < 0.0001f;
            if (wasClamped)
            {
                Debug.WriteLine($"    MATCH: N/A (target was clamped from {rawTarget:F4} to {clampedTarget:F4})");
            }
            else
            {
                Debug.WriteLine($"    MATCH: {(matches ? "YES" : "NO")} {(matches ? "✔" : "✘")}");
            }
        }
    }
}