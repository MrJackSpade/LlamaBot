using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Extensions;
using LlamaNative.Tokens.Models;
using System.Diagnostics;
using System.Text;

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
            if (SelectionHistory == null || SelectionHistory.Count == 0)
            {
                return _settings.Target;
            }

            // Calculate the sum of the probabilities of tokens in our history window (excluding the oldest one 
            // because we are simulating adding a new one to maintain the average).
            float sumExcludingFirst = SelectionHistory.Skip(1).Sum(l => l.P);

            // Calculate what the *next* token's probability needs to be to make the average of the window equal to our Target.
            // Formula: (TargetAvg * Count) - Sum(Others) = NeededValue
            float nextValue = (_settings.Target * QueueSize) - sumExcludingFirst;

            // Clamp to ensure we don't ask for impossible probabilities (e.g. > 1.0 or < 0.0)
            // MinTarget and MaxTarget allow tuning the aggressiveness of the correction.
            return Math.Clamp(nextValue, _settings.MinTarget, _settings.MaxTarget);
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

            TokenData topToken = sampleContext.OriginalCandidates.GetMostLikely();

            if (!_settings.GreedyExclude.Contains(topToken.Id))
            {
                // FORCE: Always pick this token if it's in the include list (e.g. forced endings)
                if (_settings.GreedyInclude.Contains(topToken.Id))
                {
                    topOnly = true;
                }
                // MAX-P: If a specific token exceeds its defined max probability, pick it immediately.
                // Useful for stopping generation when confidence is extremely high on a stop token.
                else if (_settings.MaxPs.TryGetValue(topToken.Id, out float maxP))
                {
                    if (topToken.P >= maxP)
                    {
                        topOnly = true;
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
                SamplingApi.SoftMax(sampleContext.Candidates, true);

                int? ts = 0;

                for (int i = 0; i < sampleContext.Candidates.Data.Length; i++)
                {
                    TokenData newToken = sampleContext.Candidates.Data.Span[i];

                    if (newToken.P > 0.001f)
                    {
                        ts++;
                    }
                    else
                    {
                        break;
                    }
                }

                // Logging and history updating
                StringBuilder? candidateBuilder = new();
                WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, candidateBuilder);

                Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] [{ts}] ({selectedToken}) T: {target:0.00}; {candidateBuilder}");
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
    }
}