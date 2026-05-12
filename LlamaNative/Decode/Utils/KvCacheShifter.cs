using LlamaNative.Apis;
using LlamaNative.Decode.Interfaces;
using LlamaNative.Decode.Models;
using LlamaNative.Exceptions;
using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Decode.Utils
{
    internal partial class KvCacheShifter(uint batchSize, SafeContextHandle handle, SafeModelHandle modelHandle) : IArrayShifter<Token>
    {
        private readonly uint _batchSize = batchSize;

        private readonly SafeContextHandle _handle = handle;

        private readonly SafeModelHandle _model = modelHandle;

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
            if (tokens.Length == 0)
            {
                return;
            }

            // llama_eval was removed upstream; re-evaluate via llama_decode (logits only needed for the last token).
            BatchDecode<int> batch = new();

            for (int i = 0; i < tokens.Length; i++)
            {
                batch.AddItem(tokens[i].Id, pos + (uint)i, [0], i == tokens.Length - 1);
            }

            if (NativeApi.Decode(_handle, batch, _batchSize) != 0)
            {
                throw new LlamaCppRuntimeError("Failed to decode (KV cache shift re-evaluation).");
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