using LlamaNative.Interop.Structs;

namespace LlamaNative.Models
{
    public class LlamaModel(SafeLlamaModelHandle handle, int vocab) : IDisposable
    {
        public SafeLlamaModelHandle Handle { get; private set; } = handle;

        public int Vocab { get; private set; } = vocab;

        public void Dispose()
        {
            ((IDisposable)Handle).Dispose();
        }
    }
}