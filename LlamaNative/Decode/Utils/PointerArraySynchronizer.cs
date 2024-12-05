using LlamaNative.Decode.Collections;
using LlamaNative.Decode.Interfaces;
using LlamaNative.Decode.Models;
using LlamaNative.Tokens.Models;
using System.Diagnostics;

namespace LlamaNative.Decode.Utils
{
    public partial class PointerArraySynchronizer(IArrayShifter shifter, Token defaultT)
    {
        protected IArrayShifter _arrayShifter = shifter;

        private readonly SequencedToken _defaultToken = new(defaultT, [0]);

        public void Sync(KvCacheState kvCache, PointerArray buffer)
        {
            this.TransformCache(kvCache, buffer);
            this.DecodeNew(kvCache, buffer);
        }

        public void TransformCache(KvCacheState kvCache, PointerArray buffer)
        {
            uint matchCount = 0;

            while (matchCount < kvCache.Length && Equals(kvCache[matchCount].Data, buffer[matchCount].Data))
            {
                matchCount++;
            }

            uint bestShiftStart = matchCount;
            uint bestShiftCount = 0;

            for (uint thisShiftStart = matchCount; thisShiftStart < kvCache.Length; thisShiftStart++)
            {
                uint thisShiftCount = 0;

                while (thisShiftStart + thisShiftCount < kvCache.Length && Equals(kvCache[thisShiftStart + thisShiftCount].Data, buffer[matchCount + thisShiftCount].Data))
                {
                    thisShiftCount++;
                }

                if (thisShiftCount > bestShiftCount)
                {
                    bestShiftCount = thisShiftCount;
                    bestShiftStart = thisShiftStart;
                }
            }

            uint shiftAmount = bestShiftStart - matchCount;

            if (shiftAmount > 0)
            {
                this.ShiftCacheTokens(kvCache, (int)bestShiftStart, (int)bestShiftCount, (int)(0 - shiftAmount));
            }

            uint clearStart = matchCount + bestShiftCount;

            if (clearStart > buffer.Pointer - 1)
            {
                //If the clear is > the buffer pointer, that means we've moved backwards (or not at all)
                //In this event, we're going to recalculate the last token so we can ensure we have the
                //correct logits. Otherwise the logits in memory will still represent the future decode
                //position.
                clearStart = buffer.Pointer - 1;
                Debug.WriteLine("Null decode. Recalculating last token.");
            }

            this.RemoveCacheTokens(kvCache, clearStart, kvCache.Length);
        }

        private void Decode(KvCacheState kvCache, BatchDecode llamaBatch)
        {
            if (llamaBatch.Items.Count > 0)
            {
                _arrayShifter.Decode(llamaBatch);

                foreach (BatchItem item in llamaBatch.Items)
                {
                    kvCache[item.Position] = new(item.Token, item.SequenceIds);
                }

                llamaBatch.Clear();
            }
        }

        private void DecodeNew(KvCacheState kvCache, PointerArray buffer)
        {
            BatchDecode llamaBatch = new();

            for (uint i = 0; i < buffer.Pointer; i++)
            {
                if (this.IsDefault(buffer[i]))
                {
                    throw new Exception("Default token found in buffer");
                }

                if (!Equals(kvCache[i].Data, buffer[i].Data))
                {
                    llamaBatch.AddItem(buffer[i].Data, i, buffer[i].SequenceIds);
                }
            }

            this.Decode(kvCache, llamaBatch);
        }

        private bool IsDefault(SequencedToken toTest)
        {
            return Equals(_defaultToken, toTest.Data);
        }

        private void RemoveCacheTokens(KvCacheState kvCache, uint clearStart, uint clearEnd)
        {
            for (uint i = clearStart; i < clearEnd; i++)
            {
                kvCache[i] = _defaultToken;
            }

            _arrayShifter.RemoveCacheTokens(clearStart, clearEnd);
        }

        private void ShiftCacheTokens(KvCacheState kvCache, int start, int count, int amount)
        {
            if (amount > 0)
            {
                throw new NotImplementedException();
            }

            this.RemoveCacheTokens(kvCache, (uint)(start + amount), (uint)start);

            for (int shift = 0; shift < count; shift++)
            {
                uint dest = (uint)(start + shift + amount);
                uint src = (uint)(start + shift);

                kvCache[dest] = kvCache[src];
                kvCache[src] = _defaultToken;
            }

            _arrayShifter.ShiftCacheTokens(0, (uint)start, (uint)(start + count), amount);
        }
    }
}