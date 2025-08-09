using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool LlamaProgressCallback(float progress, IntPtr ctx);

    /// <summary>
    /// Represents the model parameters for llama.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ModelParams
    {
        /// <summary>
        /// NULL-terminated list of devices to use for offloading (if NULL, all available devices are used)
        /// </summary>
        public IntPtr Devices;

        /// <summary>
        /// NULL-terminated list of buffer types to use for tensors that match a pattern
        /// </summary>
        public IntPtr TensorBufferTypeOverrides;

        /// <summary>
        /// Number of layers to store in VRAM.
        /// </summary>
        public int NGpuLayers;

        /// <summary>
        /// How to split the model across multiple GPUs.
        /// </summary>
        public SplitMode SplitMode;

        /// <summary>
        /// The GPU that is used for the entire model when split_mode is LLAMA_SPLIT_MODE_NONE.
        /// </summary>
        public int MainGpu;

        /// <summary>
        /// Proportion of the model (layers or rows) to offload to each GPU.
        /// </summary>
        public IntPtr TensorSplit;

        /// <summary>
        /// Called with a progress value between 0.0 and 1.0, pass NULL to disable.
        /// If the provided progress_callback returns true, model loading continues.
        /// If it returns false, model loading is immediately aborted.
        /// </summary>
        public LlamaProgressCallback ProgressCallback;

        /// <summary>
        /// Context pointer passed to the progress callback.
        /// </summary>
        public IntPtr ProgressCallbackUserData;

        /// <summary>
        /// Override key-value pairs of the model meta data.
        /// </summary>
        public IntPtr KvOverrides;

        /// <summary>
        /// Only load the vocabulary, no weights.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool VocabOnly;

        /// <summary>
        /// Use mmap if possible.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseMmap;

        /// <summary>
        /// Force system to keep model in RAM.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseMlock;

        /// <summary>
        /// Validate model tensor data.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool CheckTensors;

        /// <summary>
        /// Use extra buffer types (used for weight repacking).
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseExtraBufferTypes;
    }
}