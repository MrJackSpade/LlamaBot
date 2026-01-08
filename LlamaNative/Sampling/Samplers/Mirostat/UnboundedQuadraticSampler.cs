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
    /// A sampler that uses an Unbounded Quadratic distribution to reshape candidates around a target probability.
    /// 
    /// Key formula: Logit = PEAK - SHARPNESS * dist² / (1 + |dist|)
    /// 
    /// This provides:
    /// - Quadratic behavior near the target (good differentiation)
    /// - Linear behavior far from target (gentle unbounded tail)
    /// - No floor: logits go to -∞ as distance increases
    /// - Proper exponential suppression after softmax
    /// 
    /// State is stored in the settings object for per-channel isolation.
    /// </summary>
    public class UnboundedQuadraticSampler : BaseDynamicSampler<UnboundedQuadraticSamplerSettings>, ITokenSelector<UnboundedQuadraticSamplerSettings>
    {
        private static readonly string _logTimestamp = DateTime.Now.ToString("yyyy-MM-dd HHmmss");

        public UnboundedQuadraticSampler() : base()
        {
        }

        /// <summary>
        /// Computes the adapted target probability for the current sampling step.
        /// Uses negative feedback: target = 2 * base_target - running_average
        /// </summary>
        public static float CalculateNextTarget(UnboundedQuadraticSamplerSettings settings)
        {
            float baseTarget = settings.Target;

            // Initialize backing fields with defaults if not yet set
            // Formulas from llama_sampler_init_adaptive_p:
            // weighted_sum = target / (1.0f - decay)
            // total_weight = 1.0f / (1.0f - decay)
            if (settings.TotalWeight == 0.0f)
            {
                float clampedDecay = Math.Clamp(settings.TailDecay, 0.0f, 0.99f);
                settings.TotalWeight = 1.0f / (1.0f - clampedDecay);
                settings.WeightedSum = baseTarget / (1.0f - clampedDecay);
            }

            float target = 2.0f * baseTarget - (settings.WeightedSum / settings.TotalWeight);
            return Math.Clamp(target, settings.MinTarget, settings.MaxTarget);
        }

        public int SampleNext(SampleContext sampleContext, UnboundedQuadraticSamplerSettings settings)
        {
            // Handle negative target as bypass condition
            if (settings.Target < 0.0f)
            {
                SamplingApi.SoftMax(sampleContext.Candidates, false);
                return SamplingApi.Token(sampleContext.Candidates);
            }

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
            string greedyExcludeReason = "";
            string greedyIncludeReason = "";
            TokenData topToken = sampleContext.OriginalCandidates.GetMostLikely();

            if (settings.GreedyExclude.Contains(topToken.Id))
            {
                greedyExcludeReason = $"Top token {topToken.Id} is in GreedyExclude";
            }
            else
            {
                if (settings.GreedyInclude.Contains(topToken.Id))
                {
                    topOnly = true;
                    topOnlyReason = "Greedy Include (Forced)";
                    greedyIncludeReason = $"Top token {topToken.Id} is in GreedyInclude";
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
                : this.ApplyUnboundedQuadraticDistribution(candidates, computedTarget, sampleContext, settings);

            float originalP = sampleContext.GetOriginalData(selectedToken).P;

            // Logging (gated behind flag, grouped output)
            if (settings.Log)
            {
                this.LogUnboundedQuadraticState(sampleContext, settings, computedTarget, selectedToken, originalP, topOnly, topOnlyReason, greedyExcludeReason, greedyIncludeReason);
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
        /// Applies Unbounded Quadratic distribution to reshape candidates around the target probability.
        /// Formula: logit = PEAK - sharpness * dist² / (1 + |dist|)
        /// This provides quadratic near target, linear far away, with unbounded negative logits.
        /// </summary>
        private int ApplyUnboundedQuadraticDistribution(List<TokenData> candidates, float target, SampleContext context, UnboundedQuadraticSamplerSettings settings)
        {
            TokenDataArray candidatesArray = context.Candidates;
            Span<TokenData> candidatesSpan = candidatesArray.Data.Span;
            candidatesArray.Ordered = false;

            // Reset all logits to very low value (effectively excluded)
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

            // Unbounded Quadratic transform: logit = peak - sharpness * dist² / (1 + |dist|)
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
                        // Dirac delta case: closest token gets full peak, others excluded
                        candidatesSpan[tokenIdx].Logit = candidate.Id == closestTokenId ? settings.PeakLogitValue : -100.0f;
                    }
                    else
                    {
                        // Unbounded quadratic formula:
                        // logit = PEAK - sharpness * dist² / (1 + |dist|)
                        // - Quadratic near target (good differentiation)
                        // - Linear far from target (gentle unbounded tail)
                        // - No floor: goes to -∞ as dist increases
                        float dist = (candidate.P - target) * invWidth;
                        float absDist = Math.Abs(dist);
                        candidatesSpan[tokenIdx].Logit = settings.PeakLogitValue - settings.Sharpness * absDist * absDist / (1.0f + absDist);
                    }
                }
            }

            context.Candidates.Ordered = false;
            SamplingApi.SoftMax(context.Candidates, false);

            return SamplingApi.Token(candidatesArray);
        }

        private void LogUnboundedQuadraticState(SampleContext ctx, UnboundedQuadraticSamplerSettings settings, float computedTarget, int selectedIdx, float originalP, bool topOnly, string topOnlyReason, string greedyExcludeReason, string greedyIncludeReason)
        {
            StringBuilder sb = new();
            sb.AppendLine("========================================");

            // Bypass info
            if (topOnly)
            {
                sb.AppendLine($"[UNBOUNDED-QUADRATIC] BYPASS: {topOnlyReason}");
            }
            
            // Greedy exclude info
            if (!string.IsNullOrEmpty(greedyExcludeReason))
            {
                sb.AppendLine($"[UNBOUNDED-QUADRATIC] GREEDY EXCLUDE: {greedyExcludeReason}");
            }
            
            // Greedy include info
            if (!string.IsNullOrEmpty(greedyIncludeReason))
            {
                sb.AppendLine($"[UNBOUNDED-QUADRATIC] GREEDY INCLUDE: {greedyIncludeReason}");
            }

            // Settings section
            sb.AppendLine("[UNBOUNDED-QUADRATIC] SETTINGS:");
            sb.AppendLine($"  target (setting):       {settings.Target:F6}");
            sb.AppendLine($"  decay (setting):        {settings.TailDecay:F6}");
            sb.AppendLine($"  weighted_sum:           {settings.WeightedSum:F6}");
            sb.AppendLine($"  total_weight:           {settings.TotalWeight:F6}");
            sb.AppendLine($"  sharpness:              {settings.Sharpness:F6}");
            sb.AppendLine($"  width:                  {settings.DistributionWidth:F6}");
            sb.AppendLine($"  peak:                   {settings.PeakLogitValue:F6}");
            sb.AppendLine("----------------------------------------");

            // Input probabilities (p > 0.01)
            sb.AppendLine("[UNBOUNDED-QUADRATIC] INPUT PROBABILITIES (p > 0.01):");
            Span<TokenData> originalSpan = ctx.OriginalCandidates.Data.Span;
            for (int i = 0; i < originalSpan.Length; i++)
            {
                if (originalSpan[i].P < 0.01f)
                {
                    continue;
                }

                sb.AppendLine($"  token {originalSpan[i].Id,6}: {originalSpan[i].P:F6}");
            }

            sb.AppendLine("----------------------------------------");

            // Post-transform probabilities for same tokens
            sb.AppendLine("[UNBOUNDED-QUADRATIC] POST-TRANSFORM PROBABILITIES (same tokens):");
            Span<TokenData> transformedSpan = ctx.Candidates.Data.Span;
            
            // Build dictionary for O(1) lookups instead of O(n²)
            Dictionary<int, TokenData> transformedById = new(transformedSpan.Length);
            for (int j = 0; j < transformedSpan.Length; j++)
            {
                transformedById[transformedSpan[j].Id] = transformedSpan[j];
            }
            
            for (int i = 0; i < originalSpan.Length; i++)
            {
                if (originalSpan[i].P < 0.01f)
                {
                    continue;
                }

                int tokenId = originalSpan[i].Id;
                if (transformedById.TryGetValue(tokenId, out TokenData transformed))
                {
                    sb.AppendLine($"  token {tokenId,6}: {transformed.P:F6}");
                }
            }

            sb.AppendLine("----------------------------------------");

            // Target values
            float runningAvg = settings.TotalWeight > 0 ? settings.WeightedSum / settings.TotalWeight : settings.Target;
            float rawTarget = 2.0f * settings.Target - runningAvg;
            sb.AppendLine($"[UNBOUNDED-QUADRATIC] TARGET (calculated):     {rawTarget:F6}");
            sb.AppendLine($"[UNBOUNDED-QUADRATIC] TARGET (clipped):        {computedTarget:F6}");
            sb.AppendLine("----------------------------------------");

            // Selected token
            Token selectedToken = ctx.GetToken(TokenMask.Undefined, selectedIdx);
            sb.AppendLine($"[UNBOUNDED-QUADRATIC] SELECTED TOKEN ID:       {selectedIdx}");
            sb.AppendLine($"[UNBOUNDED-QUADRATIC] SELECTED TOKEN TEXT:     \"{selectedToken.GetEscapedValue()}\"");
            sb.AppendLine("========================================");

            // Write to file
            string logDir = Path.Combine("logs", "UnboundedQuadraticSampler", _logTimestamp);
            Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "sampler.log");
            File.AppendAllText(logPath, sb.ToString());
        }
    }
}
