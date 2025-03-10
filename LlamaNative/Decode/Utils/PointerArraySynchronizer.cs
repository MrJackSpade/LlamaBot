﻿using LlamaNative.Decode.Collections;
using LlamaNative.Decode.Interfaces;
using LlamaNative.Decode.Models;
using System.Diagnostics;

namespace LlamaNative.Decode.Utils
{
    public partial class PointerArraySynchronizer<T>(IArrayShifter<T> shifter, T defaultT)
    {
        protected IArrayShifter<T> _arrayShifter = shifter;

        private readonly T _defaultToken = defaultT;

        public void Sync(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            this.TransformCache(kvCache, buffer);
            this.DecodeNew(kvCache, buffer);
        }

        public void TransformCache(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            uint matchCount = 0;

            while (matchCount < kvCache.Length && Equals(kvCache[matchCount], buffer[matchCount]))
            {
                matchCount++;
            }

            uint bestShiftStart = matchCount;
            uint bestShiftCount = 0;

            for (uint thisShiftStart = matchCount; thisShiftStart < kvCache.Length; thisShiftStart++)
            {
                uint thisShiftCount = 0;

                while (thisShiftStart + thisShiftCount < kvCache.Length && Equals(kvCache[thisShiftStart + thisShiftCount], buffer[matchCount + thisShiftCount]))
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

        private void Decode(KvCacheState<T> kvCache, BatchDecode<T> llamaBatch)
        {
            if (llamaBatch.Items.Count > 0)
            {
                _arrayShifter.Decode(llamaBatch);

                foreach (BatchItem<T> item in llamaBatch.Items)
                {
                    kvCache[item.Position] = item.Token;
                }

                llamaBatch.Clear();
            }
        }

        private void DecodeNew(KvCacheState<T> kvCache, PointerArray<T> buffer)
        {
            BatchDecode<T> llamaBatch = new();

            for (uint i = 0; i < buffer.Pointer; i++)
            {
                if (this.IsDefault(buffer[i]))
                {
                    throw new Exception("Default token found in buffer");
                }

                if (!Equals(kvCache[i], buffer[i]))
                {
                    llamaBatch.AddItem(buffer[i], i);
                }
            }

            this.Decode(kvCache, llamaBatch);
        }

        private bool IsDefault(T toTest)
        {
            return Equals(_defaultToken, toTest);
        }

        private void RemoveCacheTokens(KvCacheState<T> kvCache, uint clearStart, uint clearEnd)
        {
            for (uint i = clearStart; i < clearEnd; i++)
            {
                kvCache[i] = _defaultToken;
            }

            _arrayShifter.RemoveCacheTokens(clearStart, clearEnd);
        }

        private void ShiftCacheTokens(KvCacheState<T> kvCache, int start, int count, int amount)
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