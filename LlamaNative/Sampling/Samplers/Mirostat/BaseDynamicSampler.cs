using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Extensions;
using System.Text;

namespace LlamaNative.Sampling.Samplers.Mirostat
{
    public abstract class BaseDynamicSampler
    {
        protected readonly Dictionary<int, bool> _isWords = [];

        protected readonly Queue<TokenData> SelectionHistory = new();

        private readonly BaseDynamicSamplerSettings _settings;

        public BaseDynamicSampler(int queueSize, BaseDynamicSamplerSettings settings)
        {
            QueueSize = queueSize;
            _settings = settings;
        }

        protected int QueueSize { get; private set; }

        protected bool IsWordCompletion(SafeLlamaModelHandle ctx, int id)
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

        protected int SelectToken(SampleContext sampleContext, float preserveWordsMinP, float preserveWordsMaxP, out bool topOnly)
        {
            SamplingApi.SoftMax(sampleContext.Candidates);
            SamplingApi.SoftMax(sampleContext.OriginalCandidates);

            topOnly = false;

            TokenData topToken = sampleContext.OriginalCandidates[0];

            if (!_settings.GreedyExclude.Contains(topToken.id))
            {
                if (_settings.GreedyInclude.Contains(topToken.id))
                {
                    topOnly = true;
                }
                else if (_settings.MaxPs.TryGetValue(topToken.id, out float maxP) && topToken.p >= maxP)
                {
                    topOnly = true;
                }
                else if (IsWordCompletion(sampleContext.ModelHandle, topToken.id))
                {
                    if (topToken.p > preserveWordsMaxP)
                    {
                        topOnly = true;
                    }
                    else
                    {
                        TokenData newTop = sampleContext.Candidates.Data.Span[0];
                        //We cant min-p unless theres at least one leftover token, which isn't
                        //true unless the top token has a high prob
                        if (newTop.p > preserveWordsMinP)
                        {
                            SamplingApi.MinP(sampleContext.Candidates, preserveWordsMinP);
                            SamplingApi.SoftMax(sampleContext.Candidates);
                        }
                    }
                }
            }

            int selectedToken;

            if (topOnly)
            {
                selectedToken = topToken.id;
            }
            else
            {
                SamplingApi.SoftMax(sampleContext.Candidates);
                selectedToken = SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
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
                avg = SelectionHistory.Average(l => l.p);
                return true;
            }
        }

        protected void WriteToLog(SampleContext sampleContext, Span<TokenData> candidateSpan, bool topOnly, int selectedToken, StringBuilder candidateBuilder)
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
                    if (candidateSpan[i].p == 0)
                    {
                        break;
                    }

                    if (i > 0)
                    {
                        candidateBuilder.Append(" | ");
                    }

                    candidateBuilder.Append(sampleContext.GetDisplayString(candidateSpan[i].id));
                }
            }

            candidateBuilder.Append(']');
        }
    }
}