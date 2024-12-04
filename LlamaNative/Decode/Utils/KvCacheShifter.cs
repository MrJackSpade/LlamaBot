using LlamaNative.Apis;
using LlamaNative.Decode.Interfaces;
using LlamaNative.Decode.Models;
using LlamaNative.Exceptions;
using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Decode.Utils
{
    internal partial class KvCacheShifter(uint threadCount, uint batchSize, SafeContextHandle handle, SafeModelHandle modelHandle) : IArrayShifter
    {
        private readonly uint _batchSize = batchSize;

        private readonly SafeContextHandle _handle = handle;

        private readonly SafeModelHandle _model = modelHandle;

        private readonly uint _threadCount = threadCount;

        public void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos)
        {
            throw new NotImplementedException();
        }

        public void Decode(BatchDecode batch)
        {
            BatchDecode idBatch = new();

            foreach (BatchItem oldItem in batch.Items)
            {
                idBatch.AddItem(oldItem.Token, oldItem.Position, oldItem.SequenceIds, oldItem.IncludeLogits);
            }

            NativeApi.Decode(_handle, idBatch, _batchSize);
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
            this.RemoveCacheTokens(index, index + 1);
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
    }
}