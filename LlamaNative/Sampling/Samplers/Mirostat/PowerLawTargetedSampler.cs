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
    /// State is stored in the settings object for per-channel isolation.
    /// </summary>
    public class PowerLawTargetedSampler : BaseDynamicSampler<PowerLawTargetedSamplerSettings>, ITokenSelector<PowerLawTargetedSamplerSettings>
    {
        public PowerLawTargetedSampler() : base()
        {
        }

        /// <summary>
        /// Computes the adapted target probability for the current sampling step.
        /// Uses negative feedback: target = 2 * base_target - running_average
        /// </summary>
        public static float CalculateNextTarget(PowerLawTargetedSamplerSettings settings)
        {
            float baseTarget = settings.Target;

            if (settings.TotalWeight == 0.0f)
            {
                return baseTarget;
            }

            float target = 2.0f * baseTarget - (settings.WeightedSum / settings.TotalWeight);
            return Math.Clamp(target, settings.MinTarget, settings.MaxTarget);
        }

        public int SampleNext(SampleContext sampleContext, PowerLawTargetedSamplerSettings settings)
        {
            // Initialize word cache from settings
            foreach (int id in settings.GreedyExclude)
            {
                settings.IsWordsCache.TryAdd(id, true);
            }

            // Softmax and prepare candidates
            SamplingApi.SoftMax(sampleContext.Candidates, false);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates, false);

            List<TokenData> candidates = this.FilterCandidates(sampleContext, settings);
            float computedTarget = CalculateNextTarget(settings);

            // Check for "Top Only" bypass conditions
            bool topOnly = false;
            string topOnlyReason = "";
            TokenData topToken = sampleContext.OriginalCandidates.GetMostLikely();

            if (!settings.GreedyExclude.Contains(topToken.Id))
            {
                if (settings.GreedyInclude.Contains(topToken.Id))
                {
                    topOnly = true;
                    topOnlyReason = "Greedy Include (Forced)";
                }
                else if (settings.MaxPs.TryGetValue(topToken.Id, out float maxP) && topToken.P >= maxP)
                {
                    topOnly = true;
                    topOnlyReason = $"Max Probability Exceeded ({topToken.P:F4} >= {maxP:F4})";
                }
                else if (this.IsWordCompletion(sampleContext.ModelHandle, topToken.Id, settings) && topToken.P > settings.PreserveWordMaxP)
                {
                    topOnly = true;
                    topOnlyReason = $"Word Preservation ({topToken.P:F4} > {settings.PreserveWordMaxP:F4})";
                }
            }

            int selectedToken = topOnly
                ? topToken.Id
                : this.ApplyPowerLawDistribution(candidates, computedTarget, sampleContext, settings);

            float originalP = sampleContext.GetOriginalData(selectedToken).P;

            // Logging (gated behind flag, grouped output)
            if (settings.Log)
            {
                this.LogPowerLawState(sampleContext, settings, computedTarget, selectedToken, originalP, topOnly, topOnlyReason);
            }

            // Update running history with exponential decay (using settings state)
            if (!topOnly || settings.FactorPreservedWords)
            {
                settings.WeightedSum = originalP + settings.TailDecay * settings.WeightedSum;
                settings.TotalWeight = 1.0f + settings.TailDecay * settings.TotalWeight;

                TokenData originalData = sampleContext.GetOriginalData(selectedToken);
                this.Push(originalData, settings);
            }

            return selectedToken;
        }

        /// <summary>
        /// Applies Power Law (Lorentzian) distribution to reshape candidates around the target probability.
        /// </summary>
        private int ApplyPowerLawDistribution(List<TokenData> candidates, float target, SampleContext context, PowerLawTargetedSamplerSettings settings)
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

                if (candidate.P >= settings.MinP)
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
            float invWidth = 1.0f / Math.Max(0.001f, settings.DistributionWidth);

            foreach (TokenData candidate in candidates)
            {
                if (candidate.P < settings.MinP)
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
                    if (settings.DistributionWidth <= float.Epsilon)
                    {
                        // Dirac delta case
                        candidatesSpan[tokenIdx].Logit = candidate.Id == closestTokenId ? settings.PeakLogitValue : -100.0f;
                    }
                    else
                    {
                        float dist = (candidate.P - target) * invWidth;
                        candidatesSpan[tokenIdx].Logit = settings.PeakLogitValue / (1.0f + dist * dist);
                    }
                }
            }

            context.Candidates.Ordered = false;
            SamplingApi.SoftMax(context.Candidates, false);

            return SamplingApi.Token(candidatesArray);
        }

        private void LogPowerLawState(SampleContext ctx, PowerLawTargetedSamplerSettings settings, float computedTarget, int selectedIdx, float originalP, bool topOnly, string topOnlyReason)
        {
            StringBuilder sb = new();
            sb.AppendLine("======================== PowerLaw Sampler ========================");

            // Bypass info
            if (topOnly)
            {
                sb.AppendLine($"  BYPASS: {topOnlyReason}");
            }

            // Target computation
            float runningAvg = settings.TotalWeight > 0 ? settings.WeightedSum / settings.TotalWeight : settings.Target;
            sb.AppendLine($"  Target: base={settings.Target:F4}, running_avg={runningAvg:F4}, computed={computedTarget:F4}");
            sb.AppendLine($"  State:  weighted_sum={settings.WeightedSum:F4}, total_weight={settings.TotalWeight:F4}, decay={settings.TailDecay:F4}");

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
            float newWeightedSum = originalP + settings.TailDecay * settings.WeightedSum;
            float newTotalWeight = 1.0f + settings.TailDecay * settings.TotalWeight;
            float newRunningAvg = newWeightedSum / newTotalWeight;
            sb.AppendLine($"  After:  weighted_sum={newWeightedSum:F4}, total_weight={newTotalWeight:F4}, running_avg={newRunningAvg:F4}");
            sb.AppendLine("==================================================================");

            Debug.Write(sb.ToString());
        }
    }
}