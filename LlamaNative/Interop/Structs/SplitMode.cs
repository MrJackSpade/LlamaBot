namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// How to split a model across multiple GPUs (<see cref="ModelParams.SplitMode"/>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C enum <c>llama_split_mode</c> in llama.cpp <c>include/llama.h</c>
    /// (<c>NONE = 0, LAYER = 1, ROW = 2</c>). 4-byte <c>int</c>.
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    public enum SplitMode
    {
        LLAMA_SPLIT_NONE = 0,

        LLAMA_SPLIT_LAYER = 1,

        LLAMA_SPLIT_ROW = 2
    }
}