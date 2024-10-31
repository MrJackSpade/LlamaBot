namespace LlamaNative.Interop.Structs
{
    public enum AttentionType
    {
        LLAMA_ATTENTION_TYPE_UNSPECIFIED = -1,

        LLAMA_ATTENTION_TYPE_CAUSAL = 0,

        LLAMA_ATTENTION_TYPE_NON_CAUSAL = 1,
    };
}