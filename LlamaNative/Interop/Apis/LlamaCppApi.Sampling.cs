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
        /// Sorts candidate tokens by their logits in descending order and calculate probabilities based on logits.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_softmax")]
        public static partial void SampleSoftMax(SafeContextHandle ctx, IntPtr candidates);

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
        /// Mirostat 1.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `int_data` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="m">The number of tokens considered in the estimation of `s_hat`. This is an arbitrary value that is used to calculate `s_hat`, which in turn helps to calculate the value of `k`. In the paper, they use `m = 100`, but you can experiment with different values to see how it affects the performance of the algorithm.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_token_mirostat")]
        public static partial int SampleTokenMirostat(SafeContextHandle ctx, IntPtr candidates, float tau, float eta, int m, float* mu);

        /// <summary>
        /// Mirostat 2.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `int_data` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_token_mirostat_v2")]
        public static partial int SampleTokenMirostatV2(SafeContextHandle ctx, IntPtr candidates, float tau, float eta, float* mu);

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
        /// Nucleus sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to TokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_sample_top_p")]
        public static partial void SampleTopP(SafeContextHandle ctx, IntPtr candidates, float p, ulong min_keep);

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