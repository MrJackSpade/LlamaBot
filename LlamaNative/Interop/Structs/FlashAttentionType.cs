namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// When to enable Flash Attention (<see cref="ContextParams.FlashAttentionType"/>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C enum <c>llama_flash_attn_type</c> in llama.cpp <c>include/llama.h</c>
    /// (<c>AUTO = -1, DISABLED = 0, ENABLED = 1</c>). 4-byte <c>int</c>.
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    public enum FlashAttentionType
    {
        Auto = -1,

        Disabled = 0,

        Enabled = 1,
    };
}