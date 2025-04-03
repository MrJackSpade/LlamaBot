using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    public abstract class SafeHandleBase : SafeHandle
    {
        private readonly Action<IntPtr> _free;

        protected SafeHandleBase(Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            _free = free;
        }

        protected SafeHandleBase(IntPtr handle, Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            this.SetHandle(handle);
            _free = free;
        }

        protected SafeHandleBase(IntPtr handle, bool ownsHandle, Action<IntPtr> free)
            : base(IntPtr.Zero, ownsHandle)
        {
            _free = free;
            this.SetHandle(handle);
        }

        public IntPtr Handle => handle;

        public override bool IsInvalid => handle == IntPtr.Zero;

        public override string ToString()
        {
            return $"0x{handle:x16}";
        }

        protected sealed override bool ReleaseHandle()
        {
            //Skip
            return true;

            _free(handle);
                this.SetHandle(IntPtr.Zero);
                return true;
        }
    }
}