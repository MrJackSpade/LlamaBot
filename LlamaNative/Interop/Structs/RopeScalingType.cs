namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Represents the RoPE scaling types for a llama context (<see cref="ContextParams.RopeScalingType"/>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C enum <c>llama_rope_scaling_type</c> in llama.cpp <c>include/llama.h</c>.
    /// The underlying type must stay a 4-byte <c>int</c> (it sits inside <see cref="ContextParams"/>, marshalled by value) —
    /// do not re-add an explicit <c>: short</c>/<c>: byte</c> base.
    /// Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    public enum RopeScalingType
    {
        Unspecified = -1,

        None = 0,

        Linear = 1,

        Yarn = 2,

        LongRope = 3,

        MAxValue = LongRope
    }
}