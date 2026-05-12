namespace LlamaNative.Interop.Settings
{
    public class ModelSettings
    {
        /// <summary>
        /// Sentinel value for <see cref="GpuLayerCount"/> that asks llama.cpp to automatically pick the number of
        /// layers to offload so the model fits in free device memory (equivalent to <c>-ngl auto</c> / <c>--fit on</c>).
        /// </summary>
        public const int AutoGpuLayerCount = -1;

        /// <summary>
        /// Number of model layers to offload to the GPU. A non-negative value selects exactly that many layers
        /// (manual selection, unchanged behaviour). Set to <see cref="AutoGpuLayerCount"/> (-1) to let llama.cpp
        /// automatically fit the layer count (and, for multi-GPU, the tensor split) to the available VRAM.
        /// </summary>
        public int GpuLayerCount { get; set; } = 0;

        public required string ModelPath { get; set; }

        public string? TensorBufferTypeOverrides { get; set; }

        public bool UseMemoryLock { get; set; } = true;

        public bool UseMemoryMap { get; set; } = false;

        /// <summary>
        /// Load only the vocabulary, no weights (useful for tokenize/detokenize-only scenarios). Default false.
        /// </summary>
        public bool VocabOnly { get; set; } = false;
    }
}