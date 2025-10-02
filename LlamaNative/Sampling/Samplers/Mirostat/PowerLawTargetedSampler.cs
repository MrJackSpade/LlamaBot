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
    public class PowerLawTargetedSampler : BaseDynamicSampler<PowerLawTargetedSamplerSettings>, ITokenSelector
    {
        public PowerLawTargetedSampler(PowerLawTargetedSamplerSettings settings) : base(settings.QueueSize, settings)
        {
            foreach (int id in _settings.GreedyExclude)
            {
                _isWords.Add(id, true);
            }
        }

        public float CalculateNextTarget()
        {
            if (SelectionHistory == null || SelectionHistory.Count == 0)
            {
                return _settings.Target;
            }

            // Calculate the sum of the values excluding the first element
            float sumExcludingFirst = SelectionHistory.Skip(1).Sum(l => l.P);

            // Calculate the next value needed to achieve the target average
            float nextValue = (_settings.Target * QueueSize) - sumExcludingFirst;

            return Math.Clamp(nextValue, _settings.MinTarget, _settings.MaxTarget);
        }

        public int SampleNext(SampleContext sampleContext)
        {
            SamplingApi.SoftMax(sampleContext.Candidates, false);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates, false);

            // Filter candidates as in original
            Span<TokenData> candidateSpan = sampleContext.Candidates.Data.Span;
            Span<TokenData> originalSpan = sampleContext.OriginalCandidates.Data.Span;

            List<TokenData> candidates = this.FilterCandidates(sampleContext);

            float target = this.CalculateNextTarget();

            // Special handling for top token
            bool topOnly = false;

            TokenData topToken = sampleContext.OriginalCandidates.GetMostLikely();

            if (!_settings.GreedyExclude.Contains(topToken.Id))
            {
                if (_settings.GreedyInclude.Contains(topToken.Id))
                {
                    topOnly = true;
                }
                else if (_settings.MaxPs.TryGetValue(topToken.Id, out float maxP))
                {
                    if (topToken.P >= maxP)
                    {
                        topOnly = true;
                    }
                }
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
            } else
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

        private int ApplyPowerLawDistribution(List<TokenData> candidates, float target, SampleContext context)
        {
            // Create a work copy of candidates to modify
            Tokens.Models.TokenDataArray candidatesArray = context.Candidates;
            Span<TokenData> candidatesSpan = candidatesArray.Data.Span;

            candidatesArray.Ordered = false;

            // Reset logits of all tokens to a very low value
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

                    if (_settings.DistributionWidth <= float.Epsilon) // Handle case where width is effectively zero
                    {
                        candidatesSpan[tokenIdx].Logit = candidate.Id == closestTokenId ? _settings.PeakLogitValue : -100.0f;
                    }
                    else
                    {
                        // Using a Power Law distribution with adjustable tail heaviness
                        float normalizedDistance = distance / Math.Max(0.001f, _settings.DistributionWidth);
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