﻿using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KvCell
    {
        /// <summary>
        /// Position in the attention mechanism.
        /// </summary>
        public int Pos;

        /// <summary>
        /// Delta value for the position.
        /// </summary>
        public int Delta;

        /// <summary>
        /// Used by recurrent state models to copy states.
        /// </summary>
        public int Src;

        /// <summary>
        /// Token value.
        /// </summary>
        public int Value;

        /// <summary>
        /// Address of the sequence id
        /// </summary>
        public long SeqId;

        /// <summary>
        /// First padding
        /// </summary>
        public int P1;

#if WINDOWS

#else
        //This is padding because the structure size differs between Windows and Linux

        public long P2;

        public long P3;

        public long P4;

        public long P5;

#endif

        public override readonly string ToString()
        {
            return $"pos: {Pos}; delt: {Delta}; value {Value}";
        }
    }

    public class SequencedKvCell(int pos, int value, int[] sequenceIds)
    {
        public int Pos { get; } = pos;
        public int Value { get; } = value;

        public int[] SequenceIds { get; } = sequenceIds;
    }
}