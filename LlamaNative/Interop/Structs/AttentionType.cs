namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Attention type used for embeddings (<see cref="ContextParams.AttentionType"/>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C enum <c>llama_attention_type</c> in llama.cpp <c>include/llama.h</c>
    /// (<c>UNSPECIFIED = -1, CAUSAL = 0, NON_CAUSAL = 1</c>; "Casual" below is a pre-existing spelling typo). 4-byte <c>int</c>.
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    public enum AttentionType
    {
        Unspecified = -1,

        Casual = 0,

        NonCasual = 1,
    };
}