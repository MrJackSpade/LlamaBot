using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    // Define the struct with explicit layout
    [StructLayout(LayoutKind.Explicit)]
    public struct KvOverride
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Key;

        [FieldOffset(128)]
        public ModelKvOverrideType Tag;

        [FieldOffset(132)] // Assuming 4 bytes for the enum (check your specific case)
        public long IntValue;

        [FieldOffset(132)] // Same offset for the union effect
        public double FloatValue;

        [FieldOffset(132)] // Same offset for the union effect
        public bool BoolValue;
    }
}