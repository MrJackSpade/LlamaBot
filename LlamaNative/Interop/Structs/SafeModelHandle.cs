namespace LlamaNative.Interop.Structs
{
    public class SafeModelHandle : SafeHandleBase
    {
        public SafeModelHandle(IntPtr handle, Action<IntPtr> free)
            : base(handle, free)
        {
        }

        protected SafeModelHandle(Action<IntPtr> free) : base(free)
        {
        }
    }
}