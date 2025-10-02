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
        public int NThreads;

        /// <summary>
        /// Number of threads to use for batch processing.
        /// </summary>
        public int NThreadsBatch;

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
        /// when to enable Flash Attention
        /// </summary>
        public FlashAttentionType FlashAttentionType;

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
        /// Defragment the KV cache if holes/size > thold, &lt;= 0 disabled (default).
        /// </summary>
        public float DefragThold;

        /// <summary>
        /// Callback for evaluation scheduling.
        /// </summary>
        public GgmlBackendSchedEvalCallback CbEval;

        /// <summary>
        /// User data for the evaluation scheduling callback.
        /// </summary>
        public IntPtr CbEvalUserData;

        /// <summary>
        /// Data type for K cache [EXPERIMENTAL].
        /// </summary>
        public GgmlType TypeK;

        /// <summary>
        /// Data type for V cache [EXPERIMENTAL].
        /// </summary>
        public GgmlType TypeV;

        /// <summary>
        /// Abort callback. If it returns true, execution of llama_decode() will be aborted. Currently works only with CPU execution.
        /// </summary>
        public GgmlAbortCallback AbortCallback;

        /// <summary>
        /// User data for the abort callback.
        /// </summary>
        public IntPtr AbortCallbackData;

        /// <summary>
        /// If true, extract embeddings (together with logits).
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool Embeddings;

        /// <summary>
        /// Offload the KQV ops (including the KV cache) to GPU.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool OffloadKQV;

        /// <summary>
        /// Measure performance timings.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool NoPerf;

        /// <summary>
        /// Offload host tensor operations to device.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool OpOffload;

        /// <summary>
        /// Use full-size SWA cache. NOTE: setting to false when n_seq_max > 1 can cause bad performance in some cases.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool SwaFull;

        /// <summary>
        /// Use a unified buffer across the input sequences when computing the attention. 
        /// Try to disable when n_seq_max > 1 for improved performance when the sequences do not share a large prefix.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool KvUnified;
    }
}