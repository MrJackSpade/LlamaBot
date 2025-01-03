﻿namespace LlamaNative.Decode.Models
{
    public class BatchItem<T>
    {
        public BatchItem()
        {
        }

        public BatchItem(T token, uint pos, int[]? seqIds = null)
        {
            Token = token;
            Position = pos;
            SequenceIds = seqIds ?? [0];
        }

        public bool IncludeLogits { get; set; }

        public uint Position { get; set; }

        public int[] SequenceIds { get; set; }

        public T Token { get; set; }

        public override string ToString()
        {
            return $"[{Position}] {Token}";
        }
    }
}