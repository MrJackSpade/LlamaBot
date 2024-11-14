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
using LlamaNative.Sampling.Models;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Interfaces;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Models
{
    public class NativeContext : INativeContext
    {
        private readonly Stack<SamplerSet> _activeSamplers = new();

        private readonly List<SamplerSet> _allSamplers;

        private readonly PointerArray<Token> _buffer;

        private readonly float[,] _embeddingStack;

        private readonly ContextParams _settings;

        private readonly PointerArraySynchronizer<Token> _synchronizer;

        private KvCacheState<Token> _kvCache;

        public uint AvailableBuffer => Size - _buffer.Pointer;

        public IReadOnlyTokenCollection Buffer => new TokenCollection(_buffer);

        public IReadOnlyTokenCollection Evaluated => new TokenCollection(_kvCache).Trim();

        public SafeContextHandle Handle { get; private set; }

        public SafeModelHandle ModelHandle { get; }

        public uint Size { get; private set; }

        private SamplerSet ActiveSamplerSet => _activeSamplers.Peek();

        private List<ISimpleSampler> ActiveSimplerSamplers => [.. ActiveSamplerSet.SimpleSamplers];

        private ITokenSelector ActiveTokenSelector => ActiveSamplerSet.TokenSelector;

        public NativeContext(SafeContextHandle handle, SafeModelHandle modelHandle, ContextParams settings, List<SamplerSet> samplerSets)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(modelHandle);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(samplerSets);

            _allSamplers = [.. samplerSets];
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

            _settings = settings;
            Size = _settings.NCtx;

            _buffer = new PointerArray<Token>(Size);
            _buffer.Fill(Token.Null);

            _kvCache = new KvCacheState<Token>(Size, Token.Null);

            ModelHandle = modelHandle;

            this.PrimeStack();
        }

        protected NativeContext()
        {
        }

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
            foreach (ISimpleSampler simpleSampler in ActiveSimplerSamplers)
            {
                simpleSampler.SampleNext(sampleContext);
            }

            logitRules.ApplyPenalty(sampleContext.Candidates);

            logitRules.ApplyBias(sampleContext.Candidates);

            logitRules.ApplyClamp(sampleContext.Candidates);

            int tokenId = ActiveTokenSelector.SampleNext(sampleContext);

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

            if (ActiveSamplerSet.Pop == token.Id)
            {
                this._activeSamplers.Pop();
            }
            else
            {
                foreach (SamplerSet samplerSet in _allSamplers)
                {
                    if (samplerSet.Push == token.Id)
                    {
                        _activeSamplers.Push(samplerSet);
                        break;
                    }
                }
            }
        }

        private void PrimeStack()
        {
            SamplerSet? defaultSet = _allSamplers.Where(s => s.Push <= 0 && s.Pop <= 0).SingleOrDefault();

            if (defaultSet is not null)
            {
                _activeSamplers.Push(defaultSet);
            }
            else
            {
                throw new ArgumentException("Sampler sets must contain one set with no push or pop operations");
            }

            List<int> allPush = _allSamplers.Select(s => s.Push).ToList();
            List<int> allPop = _allSamplers.Select(s => s.Pop).ToList();

            if (allPush.Distinct().Count() != allPush.Count)
            {
                throw new ArgumentException("Push operations must be unique");
            }

            if (allPop.Distinct().Count() != allPop.Count)
            {
                throw new ArgumentException("Pop operations must be unique");
            }
        }
    }
}