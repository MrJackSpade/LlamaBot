using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Represents a batch of data for llama.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaBatchNative
    {
        /// <summary>
        /// Number of tokens.
        /// </summary>
        public int NTokens;

        /// <summary>
        /// Pointer to the tokens array.
        /// </summary>
        public nint Token;

        /// <summary>
        /// Pointer to the embeddings array.
        /// </summary>
        public nint Embd;

        /// <summary>
        /// Pointer to the positions array.
        /// </summary>
        public nint Pos;

        /// <summary>
        /// Pointer to the sequence IDs count array.
        /// </summary>
        public nint NSeqId;

        /// <summary>
        /// Pointer to the sequence IDs array.
        /// </summary>
        public nint SeqId;

        /// <summary>
        /// Pointer to the logits array.
        /// </summary>
        public nint Logits;

        /// <summary>
        /// Used if pos is NULL. See struct comments for more details.
        /// </summary>
        public int AllPos0;

        /// <summary>
        /// Used if pos is NULL. See struct comments for more details.
        /// </summary>
        public int AllPos1;

        /// <summary>
        /// Used if seq_id is NULL. See struct comments for more details.
        /// </summary>
        public int AllSeqId;
    }
}