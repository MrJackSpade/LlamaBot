using LlamaNative.Interop.Structs;
using System.Runtime.InteropServices;

namespace LlamaNative.Interop
{
    internal unsafe partial class LlamaCppApi
    {
        /// <summary>
        /// Repetition penalty described in CTRL academic paper https://arxiv.org/abs/1909.05858, with negative logit fix.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="last_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="penalty"></param>

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_repetition_penalties")]
        public static partial void RepetitionPenalties(SafeContextHandle ctx, IntPtr candidates, [In] int[] lastTokens, ulong last_tokens_size, float penaltyRepeat, float penaltyFreq, float penaltyPresent);

        /// <summary>
        /// Tail Free Sampling described in https://www.trentonbricken.com/Tail-Free-Sampling/.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="z"></param>
        /// <param name="min_keep"></param>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_tail_free")]
        public static partial void SampleTailFree(SafeContextHandle ctx, IntPtr candidates, float z, ulong min_keep);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_temperature")]
        public static partial void SampleTemperature(SafeContextHandle ctx, IntPtr candidates, float temp);

        /// <summary>
        /// Randomly selects a token from the candidates based on their probabilities.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_token")]
        public static partial int SampleToken(SafeContextHandle ctx, IntPtr candidates);

        /// <summary>
        /// Selects the token with the highest probability.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_token_greedy")]
        public static partial int SampleTokenGreedy(SafeContextHandle ctx, IntPtr candidates);

        /// <summary>
        /// Top-K sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="k"></param>
        /// <param name="min_keep"></param>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_top_k")]
        public static partial void SampleTopK(SafeContextHandle ctx, IntPtr candidates, int k, ulong min_keep);

        /// <summary>
        /// Locally Typical Sampling implementation described in the paper https://arxiv.org/abs/2202.00666.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_typical")]
        public static partial void SampleTypical(SafeContextHandle ctx, IntPtr candidates, float p, ulong min_keep);
    }
}