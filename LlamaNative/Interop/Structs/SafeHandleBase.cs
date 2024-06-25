using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    public abstract class SafeHandleBase : SafeHandle
    {
        private readonly Action<IntPtr> _free;

        protected SafeHandleBase(Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            this._free = free;
        }

        protected SafeHandleBase(IntPtr handle, Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            this.SetHandle(handle);
            this._free = free;
        }

        protected SafeHandleBase(IntPtr handle, bool ownsHandle, Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle)
        {
            this._free = free;
            this.SetHandle(handle);
        }

        public IntPtr Handle => this.handle;

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public override string ToString() => $"0x{this.handle:x16}";

        protected override sealed bool ReleaseHandle()
        {
            this._free(this.handle);
            this.SetHandle(IntPtr.Zero);
            return true;
        }
    }
}