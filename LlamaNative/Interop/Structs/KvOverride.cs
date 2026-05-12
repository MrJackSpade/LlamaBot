using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// A single model-metadata key/value override (the array pointed to by <see cref="ModelParams.KvOverrides"/>,
    /// terminated by an entry whose <see cref="Key"/> is empty).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C struct <c>llama_model_kv_override</c> declared in llama.cpp <c>include/llama.h</c>.
    /// Field order, types and sizes must match the native struct exactly. Re-validate against <c>include/llama.h</c>
    /// whenever the bundled native libraries are updated. (Search the codebase for <c>NATIVE_STRUCT</c> to find every interop struct.)
    /// Native layout (note: upstream now puts <c>tag</c> first, then <c>key[128]</c>, then the union, 8-byte aligned at offset 136):
    /// <code>
    /// struct llama_model_kv_override {
    ///     enum llama_model_kv_override_type tag;   // offset 0   (4 bytes)
    ///     char key[128];                           // offset 4   (128 bytes, ends at 132)
    ///     union {                                  // offset 136 (8-byte aligned; 4 bytes padding at 132..135)
    ///         int64_t val_i64;
    ///         double  val_f64;
    ///         bool    val_bool;
    ///         char    val_str[128];
    ///     };                                       // 128 bytes, struct ends at 264
    /// };
    /// </code>
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi, Size = 264)]
    public struct KvOverride
    {
        /// <summary>Which member of the union is valid (<c>enum llama_model_kv_override_type tag</c>).</summary>
        [FieldOffset(0)]
        public ModelKvOverrideType Tag;

        /// <summary>The metadata key, ASCII, fixed 128-byte buffer (<c>char key[128]</c>). Empty key marks the end of the array.</summary>
        [FieldOffset(4)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Key;

        /// <summary>Union member for <see cref="ModelKvOverrideType.LlamaKvOverrideInt"/> (<c>int64_t val_i64</c>).</summary>
        [FieldOffset(136)]
        public long IntValue;

        /// <summary>Union member for <see cref="ModelKvOverrideType.LlamaKvOverrideFloat"/> (<c>double val_f64</c>).</summary>
        [FieldOffset(136)]
        public double FloatValue;

        /// <summary>Union member for <see cref="ModelKvOverrideType.LlamaKvOverrideBool"/> (<c>bool val_bool</c>).</summary>
        [FieldOffset(136)]
        [MarshalAs(UnmanagedType.I1)]
        public bool BoolValue;

        // NOTE: the union's char val_str[128] member (used by LLAMA_KV_OVERRIDE_TYPE_STR) is not represented here;
        // add a fixed-size string field at FieldOffset(136) if string overrides are ever needed.
    }
}
