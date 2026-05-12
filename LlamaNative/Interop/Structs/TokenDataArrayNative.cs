using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Native view over an array of <see cref="TokenData"/> candidates (passed by pointer to the sampling APIs,
    /// e.g. <c>llama_sampler_apply</c>; samplers may mutate <see cref="data"/>, <see cref="size"/> and <see cref="selected"/>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C struct <c>llama_token_data_array</c> declared in llama.cpp <c>include/llama.h</c>.
    /// Field order, types and sizes must match the native struct exactly. Re-validate against <c>include/llama.h</c>
    /// whenever the bundled native libraries are updated. (Search the codebase for <c>NATIVE_STRUCT</c> to find every interop struct.)
    /// Layout: <c>llama_token_data* data; size_t size; int64_t selected; bool sorted;</c>.
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct TokenDataArrayNative
    {
        /// <summary>Pointer to the first <see cref="TokenData"/> element (<c>llama_token_data*</c>).</summary>
        public nint data;

        /// <summary>Number of elements in <see cref="data"/> (<c>size_t</c>).</summary>
        public ulong size;

        /// <summary>
        /// Index into <see cref="data"/> of the selected token (<c>int64_t</c>), or -1 if none. Set by samplers; this is the array index, not the token id.
        /// </summary>
        public long selected;

        /// <summary>Whether <see cref="data"/> is currently sorted by descending logit (<c>bool</c>). Do not assume; always check.</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool sorted;
    }
}
