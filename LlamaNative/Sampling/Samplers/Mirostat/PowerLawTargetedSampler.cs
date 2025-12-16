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
    /// using a Power Law (Lorentzian) distribution to reshape the candidates.
    /// </summary>
    public class PowerLawTargetedSampler : BaseDynamicSampler<PowerLawTargetedSamplerSettings>, ITokenSelector
    {
        private float _weightedSum = 0.0f;
        private float _totalWeight = 0.0f;

        public PowerLawTargetedSampler(PowerLawTargetedSamplerSettings settings) : base(settings.QueueSize, settings)
        {
            foreach (int id in _settings.GreedyExclude)
            {
                _isWords.Add(id, true);
            }
        }

        /// <summary>
        /// Computes the adapted target probability for the current sampling step.
        /// Uses negative feedback: target = 2 * base_target - running_average
        /// </summary>
        public float CalculateNextTarget()
        {
            float baseTarget = _settings.Target;

            if (_totalWeight == 0.0f)
            {
                return baseTarget;
            }

            float target = 2.0f * baseTarget - (_weightedSum / _totalWeight);
            return Math.Clamp(target, _settings.MinTarget, _settings.MaxTarget);
        }

        public int SampleNext(SampleContext sampleContext)
        {
            // Softmax and prepare candidates
            SamplingApi.SoftMax(sampleContext.Candidates, false);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates, false);

            List<TokenData> candidates = this.FilterCandidates(sampleContext);
            float computedTarget = this.CalculateNextTarget();

            // Check for "Top Only" bypass conditions
            bool topOnly = false;
            string topOnlyReason = "";
            TokenData topToken = sampleContext.OriginalCandidates.GetMostLikely();

            if (!_settings.GreedyExclude.Contains(topToken.Id))
            {
                if (_settings.GreedyInclude.Contains(topToken.Id))
                {
                    topOnly = true;
                    topOnlyReason = "Greedy Include (Forced)";
                }
                else if (_settings.MaxPs.TryGetValue(topToken.Id, out float maxP) && topToken.P >= maxP)
                {
                    topOnly = true;
                    topOnlyReason = $"Max Probability Exceeded ({topToken.P:F4} >= {maxP:F4})";
                }
                else if (this.IsWordCompletion(sampleContext.ModelHandle, topToken.Id) && topToken.P > _settings.PreserveWordMaxP)
                {
                    topOnly = true;
                    topOnlyReason = $"Word Preservation ({topToken.P:F4} > {_settings.PreserveWordMaxP:F4})";
                }
            }

            int selectedToken = topOnly
                ? topToken.Id
                : this.ApplyPowerLawDistribution(candidates, computedTarget, sampleContext);

            float originalP = sampleContext.GetOriginalData(selectedToken).P;

            // Logging (gated behind flag, grouped output)
            if (_settings.Log)
            {
                this.LogPowerLawState(sampleContext, computedTarget, selectedToken, originalP, topOnly, topOnlyReason);
            }

            // Update running history with exponential decay
            if (!topOnly || _settings.FactorPreservedWords)
            {
                _weightedSum = originalP + _settings.TailDecay * _weightedSum;
                _totalWeight = 1.0f + _settings.TailDecay * _totalWeight;

                TokenData originalData = sampleContext.GetOriginalData(selectedToken);
                this.Push(originalData);
            }

            return selectedToken;
        }

        /// <summary>
        /// Applies Power Law (Lorentzian) distribution to reshape candidates around the target probability.
        /// </summary>
        private int ApplyPowerLawDistribution(List<TokenData> candidates, float target, SampleContext context)
        {
            TokenDataArray candidatesArray = context.Candidates;
            Span<TokenData> candidatesSpan = candidatesArray.Data.Span;
            candidatesArray.Ordered = false;

            // Reset all logits
            for (int i = 0; i < candidatesSpan.Length; i++)
            {
                candidatesSpan[i].Logit = -100.0f;
            }

            // Find closest token and track max probability for fallback
            float minDistance = float.MaxValue;
            int closestTokenId = -1;
            bool anyTokenAboveMinP = false;
            float maxP = float.MinValue;
            int maxPTokenId = -1;

            foreach (TokenData candidate in candidates)
            {
                if (candidate.P > maxP)
                {
                    maxP = candidate.P;
                    maxPTokenId = candidate.Id;
                }

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

            // Fallback if no tokens above MinP
            if (!anyTokenAboveMinP && maxPTokenId != -1)
            {
                return maxPTokenId;
            }

            // Power law transform (Lorentzian): logit = peak / (1 + dist²)
            float invWidth = 1.0f / Math.Max(0.001f, _settings.DistributionWidth);

            foreach (TokenData candidate in candidates)
            {
                if (candidate.P < _settings.MinP)
                {
                    continue;
                }

                int tokenIdx = -1;
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
                    if (_settings.DistributionWidth <= float.Epsilon)
                    {
                        // Dirac delta case
                        candidatesSpan[tokenIdx].Logit = candidate.Id == closestTokenId ? _settings.PeakLogitValue : -100.0f;
                    }
                    else
                    {
                        float dist = (candidate.P - target) * invWidth;
                        candidatesSpan[tokenIdx].Logit = _settings.PeakLogitValue / (1.0f + dist * dist);
                    }
                }
            }

            context.Candidates.Ordered = false;
            SamplingApi.SoftMax(context.Candidates, false);

            return SamplingApi.Token(candidatesArray);
        }

        private void LogPowerLawState(SampleContext ctx, float computedTarget, int selectedIdx, float originalP, bool topOnly, string topOnlyReason)
        {
            StringBuilder sb = new();
            sb.AppendLine("======================== PowerLaw Sampler ========================");

            // Bypass info
            if (topOnly)
            {
                sb.AppendLine($"  BYPASS: {topOnlyReason}");
            }

            // Target computation
            float runningAvg = _totalWeight > 0 ? _weightedSum / _totalWeight : _settings.Target;
            sb.AppendLine($"  Target: base={_settings.Target:F4}, running_avg={runningAvg:F4}, computed={computedTarget:F4}");
            sb.AppendLine($"  State:  weighted_sum={_weightedSum:F4}, total_weight={_totalWeight:F4}, decay={_settings.TailDecay:F4}");

            // Top candidates
            int topN = (int)Math.Min(4ul, ctx.OriginalCandidates.Size);
            Span<TokenData> topCandidates = ctx.OriginalCandidates.Data.Span[..topN];
            sb.Append($"  Top {topN}: ");
            for (int i = 0; i < topN; ++i)
            {
                Token token = ctx.GetToken(TokenMask.Undefined, topCandidates[i].Id);
                sb.Append($"[{topCandidates[i].Id}]\"{token.GetEscapedValue()}\"={topCandidates[i].P:F4}");
                if (i < topN - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.AppendLine();

            // Selected token
            Token selectedTok = ctx.GetToken(TokenMask.Undefined, selectedIdx);
            sb.AppendLine($"  Selected: [{selectedIdx}]\"{selectedTok.GetEscapedValue()}\" p={originalP:F4}");

            // Post-update state
            float newWeightedSum = originalP + _settings.TailDecay * _weightedSum;
            float newTotalWeight = 1.0f + _settings.TailDecay * _totalWeight;
            float newRunningAvg = newWeightedSum / newTotalWeight;
            sb.AppendLine($"  After:  weighted_sum={newWeightedSum:F4}, total_weight={newTotalWeight:F4}, running_avg={newRunningAvg:F4}");
            sb.AppendLine("==================================================================");

            Debug.Write(sb.ToString());
        }
    }
}