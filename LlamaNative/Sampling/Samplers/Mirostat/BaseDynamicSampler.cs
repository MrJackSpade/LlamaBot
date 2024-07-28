using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Extensions;
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
            if (topOnly)
            {
                candidateBuilder.Append(" [SINGLE] [");
                candidateBuilder.Append(sampleContext.GetDisplayString(selectedToken));
            }
            else
            {
                candidateBuilder.Append($"[{sampleContext.GetDisplayString(selectedToken)}] || ");

                ulong displayCount = Math.Min(10, sampleContext.Candidates.Size);

                for (int i = 0; i < (int)displayCount; i++)
                {
                    if (candidateSpan[i].P == 0)
                    {
                        break;
                    }

                    if (i > 0)
                    {
                        candidateBuilder.Append(" | ");
                    }

                    candidateBuilder.Append(sampleContext.GetDisplayString(candidateSpan[i].Id));
                }
            }

            candidateBuilder.Append(']');
        }

        protected bool IsWordCompletion(SafeModelHandle ctx, int id)
        {
            if (!_isWords.TryGetValue(id, out bool word))
            {
                string value = ctx.TokenToPiece(id);
                word = string.IsNullOrWhiteSpace(value) || !char.IsLetter(value[0]);
                _isWords[id] = word;
            }

            return !word;
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
            SamplingApi.SoftMax(sampleContext.Candidates);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates);

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
                    else
                    {
                        TokenData newTop = sampleContext.Candidates.Data.Span[0];
                        //We cant min-p unless there's at least one leftover token, which isn't
                        //true unless the top token has a high prob
                        if (newTop.P > _settings.PreserveWordMinP)
                        {
                            SamplingApi.MinP(sampleContext.Candidates, _settings.PreserveWordMinP);
                            SamplingApi.SoftMax(sampleContext.Candidates);
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
                SamplingApi.SoftMax(sampleContext.Candidates);

                if (!greedy)
                {
                    selectedToken = SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
                }
                else
                {
                    selectedToken = sampleContext.Candidates[0].Id;
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