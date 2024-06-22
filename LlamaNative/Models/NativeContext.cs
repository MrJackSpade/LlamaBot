using LlamaNative.Decode.Collections;
using LlamaNative.Decode.Decode;
using LlamaNative.Decode.Utils;
using LlamaNative.Exceptions;
using LlamaNative.Extensions;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Logit.Collections;
using LlamaNative.Logit.Extensions;
using LlamaNative.Logit.Models;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Extensions;
using LlamaNative.Tokens.Interfaces;
using LlamaNative.Tokens.Models;
using System.Diagnostics;

namespace Llama.Core
{
    public class NativeContext : INativeContext
    {
        private readonly PointerArray<Token> _buffer;

        private readonly float[,] _embeddingStack;

        private readonly KvCacheState<Token> _kvCache;

        private readonly LlamaContextSettings _settings;

        private readonly IList<ISimpleSampler> _simpleSamplers;

        private readonly PointerArraySynchronizer<Token> _synchronizer;

        private readonly ITokenSelector _tokenSelector;

        public NativeContext(SafeLlamaContextHandle handle, SafeLlamaModelHandle modelHandle, LlamaContextSettings settings, ITokenSelector tokenSelector, IEnumerable<ISimpleSampler>? simpleSamplers = null)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(modelHandle);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(tokenSelector);

            simpleSamplers ??= [];

            this._embeddingStack = new float[settings.ContextSize, 8192];

            for (int x = 0; x < settings.ContextSize; x++)
            {
                for (int y = 0; y < 8192; y++)
                {
                    this._embeddingStack[x, y] = float.NaN;
                }
            }

            _synchronizer = new PointerArraySynchronizer<Token>(
                new KvCacheShifter(settings.EvalThreadCount, settings.BatchSize, handle, modelHandle),
                new Token(-1, null)
                );

            this.Handle = handle;
            this._simpleSamplers = simpleSamplers.ToList();
            this._tokenSelector = tokenSelector;
            this._settings = settings;
            this.Size = this._settings.ContextSize;

            this._buffer = new PointerArray<Token>(this.Size);
            this._buffer.Fill(new Token(-1, null));

            this._kvCache = new KvCacheState<Token>(this.Size, new Token(-1, null));

            this.ModelHandle = modelHandle;

            if (!Directory.Exists("Logits"))
            {
                Directory.CreateDirectory("Logits");
            }
        }

        protected NativeContext()
        {
        }

        public uint AvailableBuffer => this.Size - this._buffer.Pointer;

        public IReadOnlyTokenCollection Buffer => new TokenCollection(this._buffer);

        public IReadOnlyTokenCollection Evaluated => new TokenCollection(this._kvCache);

        public SafeLlamaContextHandle Handle { get; private set; }

        public SafeLlamaModelHandle ModelHandle { get; }

        public uint Size { get; private set; }

        public void Clear()
        {
            this._buffer.Clear();
        }

        public void Dispose() => this.Handle.Dispose();

        public void Evaluate(int count = -1)
        {
            if (count != -1)
            {
                throw new NotImplementedException();
            }

            this.Ensure();

            _synchronizer.Sync(_kvCache, _buffer);
        }

        public Token SampleNext(LogitRuleCollection? logitRules = null) => this.SampleTokenRaw(logitRules);

        public Token SampleTokenRaw(LogitRuleCollection? logitRules = null)
        {
            logitRules ??= [];

            Span<float> logits = this.GetLogits();

            // Apply params.logit_bias map
            logits.Add(logitRules.OfType<LogitBias>());

            TokenDataArray candidates = new(logits);
            TokenDataArray originalCandidates = new(logits);

            SamplingApi.SuppressNonEnglish(this.ModelHandle, candidates);

            SamplingApi.SoftMax(candidates);
            SamplingApi.SoftMax(originalCandidates);

            if (candidates.Data.Span[0].logit == 0)
            {
                Debugger.Break();
            }

            Dictionary<Token, float> no_penalize = logits.Extract(this.NoPenalize());

            SampleContext sampleContext = new()
            {
                Candidates = candidates,
                ContextHandle = Handle,
                ContextTokens = Evaluated,
                ModelHandle = ModelHandle,
                OriginalCandidates = originalCandidates
            };

            logitRules.StartClamp(sampleContext.Candidates);

            //TODO: Fix cheap hack
            foreach (ISimpleSampler simpleSampler in this._simpleSamplers)
            {
                simpleSampler.SampleNext(sampleContext);
            }

            logitRules.ApplyPenalty(sampleContext.Candidates);

            sampleContext.Candidates.Update(no_penalize);

            logitRules.ApplyBias(sampleContext.Candidates);

            logitRules.ApplyClamp(sampleContext.Candidates);

            int tokenId = this._tokenSelector.SampleNext(sampleContext);

            Token toReturn = this.GetToken(tokenId);

            return toReturn;
        }

        public void SetBufferPointer(uint startIndex)
        {
            this._buffer.Pointer = startIndex;
        }

        public void Write(Token token)
        {
            if (this.AvailableBuffer == 0)
            {
                throw new OutOfContextException();
            }

            this._buffer[this._buffer.Pointer++] = token;
        }

        private TokenCollection NoPenalize()
        {
            TokenCollection collection = new();
            return collection;
        }
    }
}