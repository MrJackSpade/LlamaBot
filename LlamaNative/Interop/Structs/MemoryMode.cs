namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Managed-side KV-cache precision selector.
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — has <i>no</i> direct C counterpart in llama.cpp; it is a LlamaBot-side convenience that should be
    /// translated to <see cref="GgmlType"/> (<c>GGML_TYPE_F16</c> / <c>GGML_TYPE_F32</c>) before being placed in
    /// <see cref="ContextParams.TypeK"/>/<see cref="ContextParams.TypeV"/>. Do not marshal this type directly into a native struct.
    /// Currently unreferenced. Last reviewed: 2026-05-11.
    /// </remarks>
    public enum MemoryMode : byte
    {
        Float16,

        Float32
    }
}