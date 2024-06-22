using Llama.Data.Models;

namespace LlamaNative.Interop.Settings
{
    public class ModelSettings
    {
        public int GpuLayerCount { get; set; } = 0;

        public required string ModelPath { get; set; }

        public bool UseMemoryLock { get; set; } = false;

        public bool UseMemoryMap { get; set; } = true;
    }
}