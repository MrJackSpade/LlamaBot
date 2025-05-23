﻿using LlamaNative.Interop.Apis;
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
    public class GaussianTargetedSampler : BaseDynamicSampler<GaussianTargetedSamplerSettings>, ITokenSelector
    {
        private const float PEAK_LOGIT_VALUE = 15.0f; // Peak value for the bell curve

        public GaussianTargetedSampler(GaussianTargetedSamplerSettings settings) : base(settings.QueueSize, settings)
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
                // Apply Gaussian distribution to candidates based on proximity to target
                selectedToken = this.ApplyGaussianDistribution(candidates, target, sampleContext);
            }

            // Logging and history updating
            StringBuilder? candidateBuilder = new();
            WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, candidateBuilder);

            if (!topOnly || _settings.FactorPreservedWords)
            {
                this.Push(sampleContext.GetOriginalData(selectedToken));
            }

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] [{ts}] ({selectedToken}) T: {target:0.00}; {candidateBuilder}");

            return selectedToken;
        }

        private int ApplyGaussianDistribution(List<TokenData> candidates, float target, SampleContext context)
        {
            // Create a work copy of candidates to modify
            Tokens.Models.TokenDataArray candidatesArray = context.Candidates;
            Span<TokenData> candidatesSpan = candidatesArray.Data.Span;

            // Reset logits of all tokens to a very low value
            for (int i = 0; i < candidatesSpan.Length; i++)
            {
                candidatesSpan[i].Logit = -100.0f; // Effectively -inf for tokens below threshold
            }

            // Find the closest token to target (for potentially special handling when width ~ 0)
            float minDistance = float.MaxValue;
            int closestTokenId = -1;

            foreach (TokenData candidate in candidates)
            {
                float distance = Math.Abs(candidate.P - target);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTokenId = candidate.Id;
                }
            }

            // Apply Gaussian distribution to valid candidates
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
                    if (_settings.DistributionWidth <= float.Epsilon) // Handle case where width is effectively zero
                    {
                        candidatesSpan[tokenIdx].Logit = candidate.Id == closestTokenId ? PEAK_LOGIT_VALUE : -100.0f;
                    }
                    else
                    {
                        // Calculate Gaussian: exp(-(distance²)/(2*width²))
                        candidatesSpan[tokenIdx].Logit = (float)(PEAK_LOGIT_VALUE *
                            Math.Exp(-(distance * distance) / (2 * _settings.DistributionWidth * _settings.DistributionWidth)));
                    }
                }
            }

            // Sample using temperature sampling
            int sampled = SamplingApi.Token(candidatesArray);

            return sampled;
        }
    }
}