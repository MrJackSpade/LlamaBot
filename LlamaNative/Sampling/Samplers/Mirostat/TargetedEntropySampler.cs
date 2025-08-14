using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Extensions;
using LlamaNative.Utils.Extensions;
using System.Diagnostics;
using System.Text;

namespace LlamaNative.Sampling.Samplers.Mirostat
{
    public class TargetedEntropySampler : BaseDynamicSampler<TargetedEntropySamplerSettings>, ITokenSelector
    {
        public TargetedEntropySampler(TargetedEntropySamplerSettings settings) : base(settings.QueueSize, settings)
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
            SamplingApi.SoftMax(sampleContext.Candidates, true);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates, true);

            int? ts = 0;

            for (int i = 0; i < sampleContext.Candidates.Data.Length; i++)
            {
                TokenData newToken = sampleContext.Candidates.Data.Span[i];

                if (newToken.P > 0.001f)
                {
                    ts++;
                }
            }

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

            candidates = [.. candidates.OrderBy(c => Math.Abs(c.P - target))];

            int selectedToken = this.SelectToken(candidates, sampleContext, out bool topOnly);

            StringBuilder? candidateBuilder = new();

            WriteToLog(sampleContext, candidateSpan, topOnly, selectedToken, candidateBuilder);

            if (!topOnly || _settings.FactorPreservedWords)
            {
                this.Push(sampleContext.GetOriginalData(selectedToken));
            }

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] [{ts}] ({selectedToken}) T: {target:0.00}; {candidateBuilder}");

            TokenData originalP = sampleContext.GetOriginalData(selectedToken);

            if (originalP.P < _settings.MinP)
            {
                Debug.WriteLine("Token below min-p selected");
            }

            return selectedToken;
        }

        protected int SelectToken(List<TokenData> candidates, SampleContext sampleContext, out bool topOnly)
        {
            SamplingApi.SoftMax(sampleContext.OriginalCandidates, true);

            topOnly = false;

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
                selectedToken = candidates.First().Id;
            }

            return selectedToken;
        }
    }
}