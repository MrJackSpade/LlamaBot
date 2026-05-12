namespace LlamaNative.Interop.Structs
{
    /// <summary>
    /// Model file quantization type (passed to <c>llama_model_quantize</c>).
    /// </summary>
    /// <remarks>
    /// <b>NATIVE_STRUCT</b> — mirrors the C enum <c>llama_ftype</c> in llama.cpp <c>include/llama.h</c>. 4-byte <c>int</c>.
    /// ⚠ Only a small legacy subset is listed here — upstream has many more (Q2_K…Q6_K, IQ*, MXFP4, etc.). Add entries from
    /// <c>include/llama.h</c> as needed before using them. Last validated: 2026-05-11 — llama.cpp commit 6650c1551 (build 9129).
    /// </remarks>
    public enum LlamaFtype
    {
        LLAMA_FTYPE_ALL_F32 = 0,

        LLAMA_FTYPE_MOSTLY_F16 = 1,  // except 1d tensors

        LLAMA_FTYPE_MOSTLY_Q4_0 = 2,  // except 1d tensors

        LLAMA_FTYPE_MOSTLY_Q4_1 = 3,  // except 1d tensors

        LLAMA_FTYPE_MOSTLY_Q4_1_SOME_F16 = 4, // tok_embeddings.weight and output.weight are F16

        // LLAMA_FTYPE_MOSTLY_Q4_2 = 5,  // support has been removed
        // LLAMA_FTYPE_MOSTLY_Q4_3 (6) support has been removed
        LLAMA_FTYPE_MOSTLY_Q8_0 = 7,  // except 1d tensors

        LLAMA_FTYPE_MOSTLY_Q5_0 = 8,  // except 1d tensors

        LLAMA_FTYPE_MOSTLY_Q5_1 = 9,  // except 1d tensors
    }
}