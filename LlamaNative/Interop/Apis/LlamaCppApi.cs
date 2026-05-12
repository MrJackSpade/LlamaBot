using LlamaNative.Interop.Structs;
using System.Runtime.InteropServices;

namespace LlamaNative.Interop
{
    internal unsafe partial class LlamaCppApi
    {
#if WINDOWS

        private const string LIBRARY_NAME = "llama";

#else
        private const string LIBRARY_NAME = "libllama.so";
#endif

        static LlamaCppApi()
        {
            InitBackend();
        }

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_memory")]
        public static partial IntPtr GetMemory(SafeContextHandle ctx);

        /// <summary>
        /// Load a LoRA adapter from file. Returns the adapter pointer, or <see cref="IntPtr.Zero"/> on failure.
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_adapter_lora_init", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr AdapterLoraInit(SafeModelHandle model, string path_lora);

        /// <summary>
        /// Add a loaded LoRA adapter to the context with the given scale. Returns 0 on success.
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_set_adapter_lora")]
        public static partial int SetAdapterLora(SafeContextHandle ctx, IntPtr adapter, float scale);

        /// <summary>
        /// Free a LoRA adapter previously loaded with <see cref="AdapterLoraInit"/> (only after the context using it is freed).
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_adapter_lora_free")]
        public static partial void AdapterLoraFree(IntPtr adapter);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_context_default_params")]
        public static extern ContextParams ContextDefaultParams();

        /// <summary>
        /// Copy all tokens that belong to the specified source sequence to another destination sequence.
        /// Note that this does not allocate extra KV cache memory - it simply assigns the tokens to the new sequence.
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_memory_seq_cp")]
        public static partial void CopyCacheTokens(SafeMemoryHandle handle, int sourceSequenceId, int destinationSequenceId, int startPos, int endPos);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_decode")]
        public static partial int Decode(SafeContextHandle ctx, LlamaBatchNative batch);

        /// <summary>
        /// Frees all allocated memory
        /// </summary>
        /// <param name="ctx"></param>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_free")]
        public static partial void FreeContext(IntPtr ctx);

        /// <summary>
        /// Frees all allocated memory
        /// </summary>
        /// <param name="ctx"></param>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_model_free")]
        public static partial void FreeModel(IntPtr ctx);

        /// <summary>
        /// Get the embeddings for the input
        /// shape: [n_embd] (1-dimensional)
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_embeddings")]
        public static partial float* GetEmbeddings(SafeContextHandle ctx);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_create_tensor_buffer_type_overrides")]
        public static partial IntPtr CreateTensorBufferTypeOverrides([MarshalAs(UnmanagedType.LPUTF8Str)] string overridesStr);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_free_tensor_buffer_type_overrides")]
        public static partial void FreeTensorBufferTypeOverrides(IntPtr overridesPtr);

        /// <summary>
        /// Token logits obtained from the last call to llama_eval()
        /// The logits for the last token are stored in the last row
        /// Can be mutated in order to change the probabilities of the next token
        /// Rows: n_tokens
        /// Cols: n_vocab
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_logits")]
        public static partial float* GetLogits(SafeContextHandle ctx);

        /// <summary>
        /// Token logits obtained from the last call to llama_eval()
        /// The logits for the last token are stored in the last row
        /// Can be mutated in order to change the probabilities of the next token
        /// Rows: n_tokens
        /// Cols: n_vocab
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_logits_ith")]
        public static partial float* GetLogitsIth(SafeContextHandle ctx, int idx);


        /// <summary>
        /// not great API - very likely to change.
        /// Initialize the llama + ggml backend
        /// Call once at the start of the program
        /// </summary>
        // llama_backend_init takes no arguments; NUMA init moved to llama_numa_init(enum ggml_numa_strategy).
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_backend_init")]
        public static partial void InitBackend();

        /// <summary>
        /// Removes all tokens that do not belong to the specified sequence.
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_memory_seq_keep")]
        public static partial void KeepCacheTokens(SafeMemoryHandle handle, int sequenceId);

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// Return NULL on failure
        /// </summary>
        /// <param name="path_model"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_model_load_from_file", CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadModelFromFile(string path_model, ModelParams params_);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_model_default_params")]
        public static extern ModelParams ModelDefaultParams();

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// Return NULL on failure
        /// </summary>
        /// <param name="path_model"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_init_from_model")]
        public static extern IntPtr NewContextWithModel(SafeModelHandle mdl, ContextParams params_);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_vocab_n_tokens")]
        public static partial int NVocab(SafeVocabHandle vocab);

        /// <summary>
        /// Print system information
        /// </summary>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_print_system_info")]
        public static partial IntPtr PrintSystemInfo();

        /// <summary>
        /// Removes all tokens that belong to the specified sequence and have positions in [startPos, endPos)
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_memory_seq_rm")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool RemoveCacheTokens(SafeMemoryHandle handle, int sequenceId, int startPos, int endPos);

        /// <summary>
        /// Adds relative position "delta" to all tokens that belong to the specified sequence and have positions in [startPos, endPos)
        /// If the KV cache is RoPEd, the KV data is updated accordingly.
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_memory_seq_add")]
        public static partial void ShiftCacheTokens(SafeMemoryHandle handle, int sequenceId, int startPos, int endPos, int delta);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_model_get_vocab")]
        public static partial IntPtr GetVocab(SafeModelHandle ctx);

        /// <summary>
        /// Convert the provided text into tokens.
        /// The tokens pointer must be large enough to hold the resulting tokens.
        /// Returns the number of tokens on success, no more than n_max_tokens
        /// Returns a negative number on failure - the number of tokens that would have been returned
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="text"></param>
        /// <param name="tokens"></param>
        /// <param name="n_max_tokens"></param>
        /// <param name="add_bos"></param>
        /// <returns></returns>
        public static int Tokenize(SafeModelHandle model, string text, int[] tokens, int n_max_tokens, bool add_bos, bool special = false)
        {
            SafeVocabHandle safeVocabHandle = new(GetVocab(model), (n) => { });

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(text);
            int result;

            unsafe
            {
                fixed (byte* pUtf8Bytes = utf8Bytes)
                {
                    result = TokenizeNative(safeVocabHandle, (IntPtr)pUtf8Bytes, utf8Bytes.Length, tokens, n_max_tokens, add_bos, special);
                }
            }

            return result;
        }

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_tokenize")]
        public static partial int TokenizeNative(SafeVocabHandle vocab,
            IntPtr text,
            int textLen,
            [Out] int[] tokens,
            int maxTokens,
            [MarshalAs(UnmanagedType.Bool)] bool addBos,
            [MarshalAs(UnmanagedType.Bool)] bool special);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_token_to_piece")]
        public static partial int TokenToPiece(SafeVocabHandle vocab,
                                               int token,
                                               [Out] byte[] buf,
                                               int length,
                                               int ltrim,
                                               [MarshalAs(UnmanagedType.Bool)] bool special);

    }
}