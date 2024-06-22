using LlamaNative.Apis;
using LlamaNative.Decode.Decode;
using LlamaNative.Decode.Models;
using LlamaNative.Exceptions;
using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Decode.Utils
{
    internal partial class KvCacheShifter : IArrayShifter<Token>
    {
        private readonly uint _batchSize;

        private readonly SafeLlamaContextHandle _handle;

        private readonly SafeLlamaModelHandle _model;

        private readonly uint _threadCount;

        public KvCacheShifter(uint threadCount, uint batchSize, SafeLlamaContextHandle handle, SafeLlamaModelHandle modelHandle)
        {
            _threadCount = threadCount;
            _handle = handle;
            _model = modelHandle;
            _batchSize = batchSize;
        }

        public void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos)
        {
            throw new NotImplementedException();
        }

        public void Decode(BatchDecode<Token> batch)
        {
            BatchDecode<int> idBatch = new();

            foreach (BatchItem<Token> oldItem in batch.Items)
            {
                idBatch.AddItem(oldItem.Token.Id, oldItem.Position, oldItem.SequenceIds, oldItem.IncludeLogits);
            }

            NativeApi.Decode(_handle, idBatch, _batchSize);
        }

        public void Evaluate(Token[] tokens, uint pos)
        {
            if (_threadCount == 0)
            {
                throw new LlamaCppRuntimeError("Evaluation thread count can not be zero");
            }

            if (NativeApi.Eval(_handle, tokens.Select(l => l.Id).ToArray(), tokens.Length, pos, (int)_threadCount) != 0)
            {
                throw new LlamaCppRuntimeError("Failed to eval.");
            }
        }

        public int GetCacheTokenCount()
        {
            throw new NotImplementedException();
        }

        public void KeepCacheTokens(uint sequenceId)
        {
            throw new NotImplementedException();
        }

        public void RemoveCacheToken(uint index)
        {
            RemoveCacheTokens(index, index + 1);
        }

        public void RemoveCacheTokens(uint startPos, uint endPos)
        {
            NativeApi.RemoveCacheTokens(_handle, startPos, endPos);
        }

        public void ShiftCacheToken(uint sequenceId, uint index, int delta)
        {
            NativeApi.ShiftCacheTokens(_handle, sequenceId, index, index + 1, delta);
        }

        public void ShiftCacheTokens(uint sequenceId, uint startPos, uint endPos, int delta)
        {
            NativeApi.ShiftCacheTokens(_handle, sequenceId, startPos, endPos, delta);
        }

        public void Validate(KvCacheState<Token> kvCache)
        {
            Token[] evaluated = NativeApi.GetEvaluated(_handle, _model);

            for (int i = 0; i < kvCache.Length; i++)
            {
                if (evaluated[i] != kvCache[(uint)i])
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}