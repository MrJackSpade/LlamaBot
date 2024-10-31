using LlamaNative.Decode.Collections;
using LlamaNative.Decode.Interfaces;
using LlamaNative.Decode.Utils;
using LlamaNative.Exceptions;
using LlamaNative.Extensions;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Structs;
using LlamaNative.Logit.Collections;
using LlamaNative.Logit.Extensions;
using LlamaNative.Logit.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Interfaces;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Models
{
    public class NativeContext : INativeContext
    {
        private readonly PointerArray<Token> _buffer;

        private readonly float[,] _embeddingStack;

        private readonly ContextParams _settings;

        private readonly IList<ISimpleSampler> _simpleSamplers;

        private readonly PointerArraySynchronizer<Token> _synchronizer;

        private readonly ITokenSelector _tokenSelector;

        private KvCacheState<Token> _kvCache;

        public NativeContext(SafeContextHandle handle, SafeModelHandle modelHandle, ContextParams settings, ITokenSelector tokenSelector, IEnumerable<ISimpleSampler>? simpleSamplers = null)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(modelHandle);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(tokenSelector);

            simpleSamplers ??= [];

            _embeddingStack = new float[settings.NCtx, 8192];

            for (int x = 0; x < settings.NCtx; x++)
            {
                for (int y = 0; y < 8192; y++)
                {
                    _embeddingStack[x, y] = float.NaN;
                }
            }

            _synchronizer = new PointerArraySynchronizer<Token>(
                new KvCacheShifter(settings.NThreads, settings.NBatch, handle, modelHandle),
                Token.Null
                );

            Handle = handle;
            _simpleSamplers = simpleSamplers.ToList();
            _tokenSelector = tokenSelector;
            _settings = settings;
            Size = _settings.NCtx;

            _buffer = new PointerArray<Token>(Size);
            _buffer.Fill(Token.Null);

            _kvCache = new KvCacheState<Token>(Size, Token.Null);

            ModelHandle = modelHandle;

            if (!Directory.Exists("Logits"))
            {
                Directory.CreateDirectory("Logits");
            }
        }

        protected NativeContext()
        {
        }

        public uint AvailableBuffer => Size - _buffer.Pointer;

        public IReadOnlyTokenCollection Buffer => new TokenCollection(_buffer);

        public IReadOnlyTokenCollection Evaluated => new TokenCollection(_kvCache).Trim();

        public SafeContextHandle Handle { get; private set; }

        public SafeModelHandle ModelHandle { get; }

        public uint Size { get; private set; }

        public void Clear(bool includeCache)
        {
            _buffer.Clear();

            if (includeCache)
            {
                _kvCache = new KvCacheState<Token>(Size, Token.Null);
            }
        }

        public void Dispose()
        {
            Handle.Dispose();
        }

        public void Evaluate(int count = -1)
        {
            if (count != -1)
            {
                throw new NotImplementedException();
            }

            _synchronizer.Sync(_kvCache, _buffer);
        }

        public virtual Token SelectToken(LogitRuleCollection? logitRules, out SampleContext sampleContext)
        {
            logitRules ??= [];

            Span<float> logits = this.GetLogits();

            // Apply params.logit_bias map
            logits.Add(logitRules.OfType<LogitBias>());

            TokenDataArray candidates = new(logits);
            TokenDataArray originalCandidates = new(logits);

            sampleContext = new()
            {
                Candidates = candidates,
                ContextHandle = Handle,
                ContextTokens = Evaluated,
                ModelHandle = ModelHandle,
                OriginalCandidates = originalCandidates
            };

            logitRules.StartClamp(sampleContext.Candidates);

            //TODO: Fix cheap hack
            foreach (ISimpleSampler simpleSampler in _simpleSamplers)
            {
                simpleSampler.SampleNext(sampleContext);
            }

            logitRules.ApplyPenalty(sampleContext.Candidates);

            logitRules.ApplyBias(sampleContext.Candidates);

            logitRules.ApplyClamp(sampleContext.Candidates);

            int tokenId = _tokenSelector.SampleNext(sampleContext);

            Token toReturn = this.GetToken(TokenMask.Bot, tokenId);

            return toReturn;
        }

        public void SetBufferPointer(uint startIndex)
        {
            _buffer.Pointer = startIndex;
        }

        public void Write(Token token)
        {
            if (AvailableBuffer == 0)
            {
                throw new OutOfContextException();
            }

            _buffer[_buffer.Pointer++] = token;
        }
    }
}