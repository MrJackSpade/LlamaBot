using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Represents a batch of input data for llama (passed by value to <c>llama_decode</c> / <c>llama_encode</c>).
    /// All pointer fields point to caller-owned arrays of length <see cref="NTokens"/> (except <see cref="SeqId"/>,
    /// which is an array of <c>llama_seq_id*</c>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C struct <c>llama_batch</c> declared in llama.cpp <c>include/llama.h</c>.
    /// Field order, types and sizes must match the native struct exactly. Re-validate against <c>include/llama.h</c>
    /// whenever the bundled native libraries are updated. (Search the codebase for <c>NATIVE_STRUCT</c> to find every interop struct.)
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
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
        /// Pointer to the logits/output array.
        /// </summary>
        public nint Logits;
    }
}