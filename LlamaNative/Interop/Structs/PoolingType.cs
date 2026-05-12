namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Embedding pooling type for a llama context (<see cref="ContextParams.PoolingType"/>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C enum <c>llama_pooling_type</c> in llama.cpp <c>include/llama.h</c>. 4-byte <c>int</c>.
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    public enum PoolingType
    {
        Unspecified = -1,

        None = 0,

        Mean = 1,

        Cls = 2,

        Last = 3,

        Rank = 4
    }
}
