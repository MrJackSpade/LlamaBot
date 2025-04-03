namespace LlamaNative.Interop.Structs
{
    public class SafeVocabHandle : SafeHandleBase
    {
        public SafeVocabHandle(IntPtr handle, Action<IntPtr> free)
            : base(handle, free)
        {
        }

        protected SafeVocabHandle(Action<IntPtr> free) : base(free)
        {
        }
    }
}