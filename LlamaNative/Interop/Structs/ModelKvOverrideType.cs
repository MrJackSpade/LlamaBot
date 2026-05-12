namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Tag for the union in <see cref="KvOverride"/>.
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C enum <c>llama_model_kv_override_type</c> in llama.cpp <c>include/llama.h</c>
    /// (<c>LLAMA_KV_OVERRIDE_TYPE_INT, _FLOAT, _BOOL, _STR</c>). Underlying type is a 4-byte <c>int</c>.
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    public enum ModelKvOverrideType
    {
        /// <summary><c>LLAMA_KV_OVERRIDE_TYPE_INT</c> — use <see cref="KvOverride.IntValue"/>.</summary>
        LlamaKvOverrideInt,

        /// <summary><c>LLAMA_KV_OVERRIDE_TYPE_FLOAT</c> — use <see cref="KvOverride.FloatValue"/>.</summary>
        LlamaKvOverrideFloat,

        /// <summary><c>LLAMA_KV_OVERRIDE_TYPE_BOOL</c> — use <see cref="KvOverride.BoolValue"/>.</summary>
        LlamaKvOverrideBool,

        /// <summary><c>LLAMA_KV_OVERRIDE_TYPE_STR</c> — string override (the union's <c>char val_str[128]</c>; not currently represented in <see cref="KvOverride"/>).</summary>
        LlamaKvOverrideStr
    }
}
