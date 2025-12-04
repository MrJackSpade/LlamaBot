namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Represents the RoPE scaling types for a llama context.
    /// </summary>
    public enum RopeScalingType : short
    {
        Unspecified = -1,

        None = 0,

        Linear = 1,

        Yarn = 2,

        LongRope = 3,

        MAxValue = LongRope
    }
}