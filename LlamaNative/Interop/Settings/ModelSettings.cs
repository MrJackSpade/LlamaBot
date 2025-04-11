namespace LlamaNative.Interop.Settings
{
    public class ModelSettings
    {
        public int GpuLayerCount { get; set; } = 0;

        public required string ModelPath { get; set; }

        public bool UseMemoryLock { get; set; } = true;

        public bool UseMemoryMap { get; set; } = false;

        public string? TensorBufferTypeOverrides { get; set; }
    }
}