using LlamaNative.Interop.Structs;

namespace LlamaNative.Models
{
    public class Model(SafeModelHandle handle, int vocab) : IDisposable
    {
        public SafeModelHandle Handle { get; private set; } = handle;

        public int Vocab { get; private set; } = vocab;

        public void Dispose()
        {
            ((IDisposable)Handle).Dispose();
        }
    }
}