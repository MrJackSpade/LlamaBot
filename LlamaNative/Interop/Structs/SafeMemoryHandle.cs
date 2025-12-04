namespace LlamaNative.Interop.Structs
{
    public class SafeMemoryHandle : SafeHandleBase
    {
        public SafeMemoryHandle(nint memoryPtr, Action<nint> free)
            : base(memoryPtr, free)
        {
        }

        protected SafeMemoryHandle(Action<nint> free) : base(free)
        {
        }
    }
}