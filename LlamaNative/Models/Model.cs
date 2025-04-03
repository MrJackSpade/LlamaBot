using LlamaNative.Interop.Structs;

namespace LlamaNative.Models
{
    public class Model(SafeModelHandle handle, SafeVocabHandle vocab, int nvocab) : IDisposable
    {
        public SafeModelHandle Handle { get; private set; } = handle;

        public int NVocab { get; private set; } = nvocab;

        public SafeVocabHandle Vocab { get; private set; } = vocab;

        public void Dispose()
        {
            ((IDisposable)Handle).Dispose();
        }
    }
}