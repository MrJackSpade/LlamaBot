namespace LlamaNative.Interop.Structs
{
    public class SafeContextHandle : SafeHandleBase
    {
        public SafeContextHandle(nint contextPtr, Action<nint> free)
            : base(contextPtr, free)
        {
        }

        protected SafeContextHandle(Action<nint> free) : base(free)
        {
        }
    }
}