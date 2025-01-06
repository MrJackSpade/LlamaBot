using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool GgmlAbortCallback(IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool GgmlBackendSchedEvalCallback(IntPtr tensor, bool ask, IntPtr userData);

    /// <summary>
    /// Represents the parameters for a llama context.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContextParams
    {
        /// <summary>
        /// Text context, 0 = from model.
        /// </summary>
        public uint NCtx;

        /// <summary>
        /// Logical maximum batch size that can be submitted to llama_decode.
        /// </summary>
        public uint NBatch;

        /// <summary>
        /// Physical maximum batch size.
        /// </summary>
        public uint NUBatch;

        /// <summary>
        /// Max number of sequences (i.e. distinct states for recurrent models).
        /// </summary>
        public uint NSeqMax;

        /// <summary>
        /// Number of threads to use for generation.
        /// </summary>
        public uint NThreads;

        /// <summary>
        /// Number of threads to use for batch processing.
        /// </summary>
        public uint NThreadsBatch;

        /// <summary>
        /// RoPE scaling type, from `LlamaRopeScalingType`.
        /// </summary>
        public RopeScalingType RopeScalingType;

        /// <summary>
        /// Whether to pool (sum) embedding results by sequence id (ignored if no pooling layer).
        /// </summary>
        public PoolingType PoolingType;

        /// <summary>
        /// Attention type to use for embeddings
        /// </summary>
        public AttentionType AttentionType;

        /// <summary>
        /// RoPE base frequency, 0 = from model.
        /// </summary>
        public float RopeFreqBase;

        /// <summary>
        /// RoPE frequency scaling factor, 0 = from model.
        /// </summary>
        public float RopeFreqScale;

        /// <summary>
        /// YaRN extrapolation mix factor, negative = from model.
        /// </summary>
        public float YarnExtFactor;

        /// <summary>
        /// YaRN magnitude scaling factor.
        /// </summary>
        public float YarnAttnFactor;

        /// <summary>
        /// YaRN low correction dim.
        /// </summary>
        public float YarnBetaFast;

        /// <summary>
        /// YaRN high correction dim.
        /// </summary>
        public float YarnBetaSlow;

        /// <summary>
        /// YaRN original context size.
        /// </summary>
        public uint YarnOrigCtx;

        /// <summary>
        /// Defragment the KV cache if holes/size > thold, < 0 disabled (default).
        /// </summary>
        public float DefragThold;

        /// <summary>
        /// Callback for evaluation scheduling.
        /// </summary>
        public GgmlBackendSchedEvalCallback CbEval;

        /// <summary>
        /// User data for the evaluation scheduling callback.
        /// </summary>
        public nint CbEvalUserData;

        /// <summary>
        /// Data type for K cache.
        /// </summary>
        public GgmlType TypeK;

        /// <summary>
        /// Data type for V cache.
        /// </summary>
        public GgmlType TypeV;

        /// <summary>
        /// The llama_decode() call computes all logits, not just the last one (DEPRECATED - set llama_batch.logits instead).
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool LogitsAll;

        /// <summary>
        /// If true, extract embeddings (together with logits).
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool Embeddings;

        /// <summary>
        /// Whether to offload the KQV ops (including the KV cache) to GPU.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool OffloadKQV;

        /// <summary>
        /// Whether to use flash attention.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool FlashAttn;

        /// <summary>
        /// Whether to use flash attention.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool NoPerf;

        /// <summary>
        /// Abort callback. If it returns true, execution of llama_decode() will be aborted. Currently works only with CPU execution.
        /// </summary>
        public GgmlAbortCallback AbortCallback;

        /// <summary>
        /// User data for the abort callback.
        /// </summary>
        public IntPtr AbortCallbackData;
    }
}