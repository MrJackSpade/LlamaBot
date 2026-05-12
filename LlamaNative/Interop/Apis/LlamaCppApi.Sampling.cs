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

    }
}