using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using LlamaNative.Utils.Extensions;
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
            SamplingApi.SoftMax(sampleContext.Candidates);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates);

            int? ts = 0;

            for (int i = 0; i < sampleContext.Candidates.Data.Length; i++)
            {
                TokenData newToken = sampleContext.Candidates.Data.Span[i];

                if (newToken.P > 0.001f)
                {
                    ts++;
                }
            }

            // Filter candidates as in original
            Span<TokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            List<TokenData> candidates = [.. candidateSpan.Where(c =>
                c.P >= _settings.MinP &&
                sampleContext.GetOriginalData(c.Id).P >= _settings.MinP
            )];

            if (candidates.Count == 0)
            {
                candidates.Add(candidateSpan[0]);
            }

            float target = this.CalculateNextTarget();

            // Special handling for top token
            bool topOnly = false;
            TokenData topToken = sampleContext.OriginalCandidates[0];

            if (!_settings.GreedyExclude.Contains(topToken.Id))
            {
                if (_settings.GreedyInclude.Contains(topToken.Id))
                {
                    topOnly = true;
                }
                else if (_settings.MaxPs.TryGetValue(topToken.Id, out float maxP) && topToken.P >= maxP)
                {
                    topOnly = true;
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

            // Logging and history updating
            StringBuilder? candidateBuilder = new();
            WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, candidateBuilder);

            if (!topOnly || _settings.FactorPreservedWords)
            {
                var originalP = sampleContext.GetOriginalData(selectedToken);
                this.Push(originalP);
            }

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] [{ts}] ({selectedToken}) T: {target:0.00}; {candidateBuilder}");

            return selectedToken;
        }

        private int ApplyPowerLawDistribution(List<TokenData> candidates, float target, SampleContext context)
        {
            // Create a work copy of candidates to modify
            var candidatesArray = context.Candidates;
            var candidatesSpan = candidatesArray.Data.Span;

            // Reset logits of all tokens to a very low value
            for (int i = 0; i < candidatesSpan.Length; i++)
            {
                candidatesSpan[i].Logit = -100.0f; // Effectively -inf for tokens below threshold
            }

            // Find the closest token to target (for potentially special handling when width ~ 0)
            float minDistance = float.MaxValue;
            int closestTokenId = -1;

            foreach (var candidate in candidates)
            {
                float distance = Math.Abs(candidate.P - target);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTokenId = candidate.Id;
                }
            }

            // Apply Power Law distribution to valid candidates
            foreach (var candidate in candidates)
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

            // Sample using temperature sampling
            int sampled = SamplingApi.Token(candidatesArray);

            return sampled;
        }
    }
}