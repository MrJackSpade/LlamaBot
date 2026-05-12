using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// A single candidate token with its logit and probability (element type of <see cref="TokenDataArrayNative"/>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C struct <c>llama_token_data</c> declared in llama.cpp <c>include/llama.h</c>.
    /// Field order, types and sizes must match the native struct exactly. Re-validate against <c>include/llama.h</c>
    /// whenever the bundled native libraries are updated. (Search the codebase for <c>NATIVE_STRUCT</c> to find every interop struct.)
    /// Layout: <c>int32_t id; float logit; float p;</c>. Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct TokenData(int id, float logit, float p)
    {
        /// <summary>
        /// token id
        /// </summary>
        public int Id = id;

        /// <summary>
        /// log-odds of the token
        /// </summary>
        public float Logit = logit;

        /// <summary>
        /// probability of the token
        /// </summary>
        public float P = p;
    }
}