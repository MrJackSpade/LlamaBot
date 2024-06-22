using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TokenDataArrayNative
    {
        public nint data;

        public ulong size;

        public bool sorted;
    }
}