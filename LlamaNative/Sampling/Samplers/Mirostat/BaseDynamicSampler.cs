using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Extensions;
using System.Text;

namespace LlamaNative.Sampling.Samplers.Mirostat
{
    public abstract class BaseDynamicSampler<TSettings>(int queueSize, TSettings settings) where TSettings : BaseDynamicSamplerSettings
    {
        protected readonly Dictionary<int, bool> _isWords = [];

        protected readonly TSettings _settings = settings;

        protected readonly Queue<TokenData> SelectionHistory = new();

        protected int QueueSize { get; private set; } = queueSize;

        protected static void WriteToLog(SampleContext sampleContext, Span<TokenData> candidateSpan, bool topOnly, int selectedToken, StringBuilder candidateBuilder)
        {
            Dictionary<int, float> originalPs = sampleContext.OriginalCandidates.Data.ToArray().ToDictionary(x => x.Id, x => x.P);

            if (topOnly)
            {
                candidateBuilder.Append(" [SINGLE] [");
                candidateBuilder.Append(sampleContext.GetDisplayString(selectedToken));
            }
            else
            {
                candidateBuilder.Append($"[{sampleContext.GetDisplayString(selectedToken)}] || ");

                ulong displayCount = Math.Min(10, sampleContext.Candidates.Size);

                int i = 0;
                ulong d = 0;

                do
                {
                    if (d > displayCount || i >= sampleContext.Candidates.Data.Length)
                    {
                        break;
                    }

                    float p = candidateSpan[i].P;
                    float originalP = originalPs[candidateSpan[i].Id];

                    if (p < 0.001 && originalP < 0.001)
                    {
                        i++;
                        continue;
                    }

                    d++;

                    if (i > 0)
                    {
                        candidateBuilder.Append(" | ");
                    }

                    candidateBuilder.Append(sampleContext.GetDisplayString(candidateSpan[i].Id));

                    i++;
                } while (true);
            }

            candidateBuilder.Append(']');
        }

        protected List<TokenData> FilterCandidates(SampleContext sampleContext)
        {
            List<TokenData> toReturn = [];

            Span<TokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            for (int i = 0; i < candidateSpan.Length; i++)
            {
                TokenData token = candidateSpan[i];

                //This accounts for adjustments based on samplers
                if (token.P < _settings.MinP)
                {
                    continue;
                }

                TokenData originalTokenData = sampleContext.GetOriginalData(token.Id);

                //This ensures a minimum probability for the original token
                if (originalTokenData.P < _settings.MinP)
                {
                    continue;
                }

                if (_settings.MinPs.TryGetValue(token.Id, out float minP))
                {
                    if (originalTokenData.P < minP)
                    {
                        continue;
                    }
                }

                toReturn.Add(token);
            }

            if (toReturn.Count == 0)
            {
                toReturn.Add(sampleContext.Candidates[0]);
            }

            return toReturn;
        }

        protected bool IsWordCompletion(SafeModelHandle ctx, int id)
        {
            if (!_isWords.TryGetValue(id, out bool newWordStart))
            {
                string value = ctx.TokenToPiece(id);
                bool emptyToken = string.IsNullOrWhiteSpace(value);
                bool nonLetterStart = !char.IsLetter(value[0]);

                // Ex "As" is the start of a new line, and has no space, but is still a new word
                bool pascalCase = char.IsUpper(value[0]) && value.Length > 1 && char.IsLower(value[1]);

                newWordStart = emptyToken || nonLetterStart || pascalCase;

                _isWords[id] = newWordStart;
            }

            return !newWordStart;
        }

        protected void Push(TokenData token)
        {
            SelectionHistory.Enqueue(token);

            if (SelectionHistory.Count > QueueSize)
            {
                SelectionHistory.Dequeue();
            }
        }

        protected int SelectToken(SampleContext sampleContext, bool greedy)
        {
            return this.SelectToken(sampleContext, greedy, out _);
        }

        protected int SelectToken(SampleContext sampleContext, bool greedy, out bool topOnly)
        {
            SamplingApi.SoftMax(sampleContext.Candidates, false);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates, false);

            topOnly = false;

            TokenData topToken = sampleContext.OriginalCandidates.GetMostLikely();

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
                    else
                    {
                        TokenData newTop = sampleContext.Candidates.Data.Span[0];
                        //We cant min-p unless there's at least one leftover token, which isn't
                        //true unless the top token has a high prob
                        if (newTop.P > _settings.PreserveWordMinP)
                        {
                            SamplingApi.MinP(sampleContext.Candidates, _settings.PreserveWordMinP);
                        }
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
                if (!greedy)
                {
                    selectedToken = SamplingApi.Token(sampleContext.Candidates);
                }
                else
                {
                    selectedToken = sampleContext.Candidates.GetMostLikely().Id;
                }
            }

            return selectedToken;
        }

        protected bool TryGetQueueAverage(out float avg)
        {
            avg = 0f;
            if (SelectionHistory.Count < QueueSize)
            {
                return false;
            }
            else
            {
                avg = SelectionHistory.Average(l => l.P);
                return true;
            }
        }
    }
}