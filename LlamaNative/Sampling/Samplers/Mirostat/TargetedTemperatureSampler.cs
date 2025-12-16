using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Extensions;
using System.Diagnostics;
using System.Text;

namespace LlamaNative.Sampling.Samplers.Mirostat
{
    public class TargetedTemperatureSampler : BaseDynamicSampler<TargetedTemperatureSamplerSettings>, ITokenSelector<TargetedTemperatureSamplerSettings>
    {
        public TargetedTemperatureSampler() : base()
        {
        }

        public void ApplyOriginalMinP(SampleContext context, TargetedTemperatureSamplerSettings settings)
        {
            SamplingApi.SoftMax(context.Candidates, true);

            Dictionary<int, int>? mapping = [];

            Span<TokenData> newData = context.Candidates.Data.Span;

            for (int i = 0; i < context.Candidates.Data.Length; i++)
            {
                TokenData newToken = newData[i];
                mapping.Add(newToken.Id, i);
            }

            foreach (TokenData token in context.OriginalCandidates)
            {
                float minP = settings.MinP;

                if (settings.MinPs.TryGetValue(token.Id, out float cminp))
                {
                    minP = Math.Max(minP, cminp);
                }

                if (token.P < minP)
                {
                    int newIndex = mapping[token.Id];

                    //Don't apply it to the most likely new token.
                    if (newIndex > 0)
                    {
                        context.Candidates.SetLogitAtIndex(newIndex, float.NegativeInfinity);
                    }
                }
            }
        }

        public float CalculateNextTarget(TargetedTemperatureSamplerSettings settings)
        {
            if (settings.SelectionHistory == null || settings.SelectionHistory.Count == 0)
            {
                throw new ArgumentException("Values list cannot be null or empty.");
            }

            // Calculate the sum of the values excluding the first element
            float sumExcludingFirst = settings.SelectionHistory.Skip(1).Sum(l => l.P);

            // Calculate the next value needed to achieve the target average
            float nextValue = (settings.Target * settings.QueueSize) - sumExcludingFirst;

            return nextValue;
        }

        public int SampleNext(SampleContext sampleContext, TargetedTemperatureSamplerSettings settings)
        {
            // Initialize word cache from settings
            foreach (int id in settings.GreedyExclude)
            {
                settings.IsWordsCache.TryAdd(id, true);
            }

            int? ts = 0;

            for (int i = 0; i < sampleContext.Candidates.Data.Length; i++)
            {
                TokenData newToken = sampleContext.Candidates.Data.Span[i];

                if (newToken.P > 0.001f)
                {
                    ts++;
                }
            }

            //SoftMax for backup
            this.ApplyOriginalMinP(sampleContext, settings);
            SamplingApi.SoftMax(sampleContext.Candidates, true);

            Span<TokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            float sampleTemp = settings.Temperature;
            float average = 0f;

            if (this.TryGetQueueAverage(settings, out average) &&
                settings.Temperature > 0)
            {
                float totalDiff = 0;

                float c_target = settings.CurrentTarget;
                float c_min = float.MaxValue;
                float c_max = float.MinValue;

                //Find the real target range. This is important because if we're
                //lower than the lowest token, we actually scale back to normal distribution
                //likewise with max
                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    TokenData token = candidateSpan[i];

                    if (token.P > settings.MinP)
                    {
                        c_min = Math.Min(token.P, c_min);
                    }

                    c_max = Math.Max(token.P, c_max);
                }

                //clamp the target to the real range
                c_target = Math.Max(c_target, c_min);
                c_target = Math.Min(c_target, c_max);

                //Find the difference total difference between the real target and
                //each token
                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    TokenData token = candidateSpan[i];
                    totalDiff += Math.Abs(token.P - c_target);
                }

                //Now calculate the proportion of the distance and apply scaling
                float scaledTotalDiff = totalDiff * (float)Math.Exp(1 - settings.Scale);
                for (int i = 0; i < candidateSpan.Length; i++)
                {
                    TokenData token = candidateSpan[i];
                    float diff = token.P - c_target;
                    float absDiff = Math.Abs(diff);
                    float scaledDiff = absDiff * (float)Math.Exp(1 - settings.Scale);
                    float perDiff = scaledDiff / scaledTotalDiff;
                    float perInvDiff = 1 - perDiff;
                    float adjTemp = sampleTemp / perInvDiff;
                    SamplingApi.Temperature(sampleContext.Candidates, i, adjTemp);
                }
            }
            else if (sampleTemp != 0)
            {
                SamplingApi.Temperature(sampleContext.Candidates, sampleTemp);
            }

            SamplingApi.TailFree(sampleContext.Candidates, settings.Tfs, 1);

            int selectedToken = this.SelectToken(sampleContext, settings, settings.Temperature <= 0, out bool topOnly);

            // Compute error as the difference between observed surprise and target surprise value

            StringBuilder? candidateBuilder = new();

            WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, candidateBuilder);

            if (!topOnly || settings.FactorPreservedWords)
            {
                if (this.TryGetQueueAverage(settings, out average))
                {
                    settings.CurrentTarget = this.CalculateNextTarget(settings);
                    settings.CurrentTarget = Math.Clamp(settings.CurrentTarget, settings.MinTarget, settings.MaxTarget);
                }

                this.Push(sampleContext.GetOriginalData(selectedToken), settings);
            }

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] [{ts}] ({selectedToken}) T: {settings.CurrentTarget:0.00}; Avg: {average:0.00}; {candidateBuilder}");

            TokenData originalP = sampleContext.GetOriginalData(selectedToken);

            if (originalP.P < settings.MinP)
            {
                Debug.WriteLine("Token below min-p selected");
            }

            return selectedToken;
        }
    }
}