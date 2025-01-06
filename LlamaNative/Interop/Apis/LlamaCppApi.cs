using LlamaNative.Interop.Structs;
using System.Diagnostics.CodeAnalysis;
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
            InitBackend(false);
        }

        /// <summary>
        /// Apply a LoRA adapter to a loaded model
        /// path_base_model is the path to a higher quality model to use as a base for
        /// the layers modified by the adapter. Can be NULL to use the current loaded model.
        /// The model needs to be reloaded before applying a new adapter, otherwise the adapter
        /// will be applied on top of the previous one
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="path_lora"></param>
        /// <param name="path_base_model"></param>
        /// <param name="n_threads"></param>
        /// <returns>Returns 0 on success</returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_apply_lora_from_file", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int ApplyLoraFromFile(SafeContextHandle ctx, string path_lora, string path_base_model, int n_threads);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_context_default_params")]
        [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
        public static extern ContextParams ContextDefaultParams();

        /// <summary>
        /// Copy all tokens that belong to the specified source sequence to another destination sequence.
        /// Note that this does not allocate extra KV cache memory - it simply assigns the tokens to the new sequence.
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_kv_cache_seq_cp")]
        public static partial void CopyCacheTokens(SafeContextHandle handle, int sourceSequenceId, int destinationSequenceId, int startPos, int endPos);

        /// <summary>
        /// Copies the state to the specified destination address.
        /// Destination needs to have allocated enough memory.
        /// Returns the number of bytes copied
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_copy_state_data")]
        public static partial ulong CopyStateData(SafeContextHandle ctx, [Out] byte[] dest);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_decode")]
        public static partial int Decode(SafeContextHandle ctx, LlamaBatchNative batch);

        /// <summary>
        /// Run the llama inference to obtain the logits and probabilities for the next token.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tokens">The new tokens to process</param>
        /// <param name="n_tokens">The number of new tokens to process</param>
        /// <param name="n_past">The number of tokens to use from previous eval calls</param>
        /// <param name="n_threads"></param>
        /// <returns>Returns 0 on success</returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_eval")]
        public static partial int Eval(SafeContextHandle ctx, [In] int[] tokens, int n_tokens, int n_past, int n_threads);

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
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_free_model")]
        public static partial void FreeModel(IntPtr ctx);

        /// <summary>
        /// Returns the number of tokens in the KV cache
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_kv_cache_token_count")]
        public static partial int GetCacheTokenCount(SafeContextHandle ctx);

        /// <summary>
        /// Get the embeddings for the input
        /// shape: [n_embd] (1-dimensional)
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_embeddings")]
        public static partial float* GetEmbeddings(SafeContextHandle ctx);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_kv_cache")]
        public static partial IntPtr GetKvCache(SafeContextHandle ctx);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_kv_cell_seq_id_count")]
        public static partial IntPtr GetKVCellSeqIdCount(IntPtr cell);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_kv_cell_seq_ids")]
        public static partial void GetKvCellSeqIds(IntPtr cell, [Out] int[] seqIds);

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
        /// Returns the maximum size in bytes of the state (rng, logits, embedding
        /// and kv_cache) - will often be smaller after compacting tokens
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_get_state_size")]
        public static partial ulong GetStateSize(SafeContextHandle ctx);

        /// <summary>
        /// not great API - very likely to change.
        /// Initialize the llama + ggml backend
        /// Call once at the start of the program
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_backend_init")]
        public static partial void InitBackend([MarshalAs(UnmanagedType.Bool)] bool numa);

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// Return NULL on failure
        /// </summary>
        /// <param name="path_model"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_init_from_file", CharSet = CharSet.Unicode)]
        [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
        public static extern IntPtr InitFromFile(string path_model, ContextParams params_);

        /// <summary>
        /// Removes all tokens that do not belong to the specified sequence.
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_kv_cache_seq_keep")]
        public static partial void KeepCacheTokens(SafeContextHandle handle, int sequenceId);

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// Return NULL on failure
        /// </summary>
        /// <param name="path_model"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_load_model_from_file", CharSet = CharSet.Ansi)]
        [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
        public static extern IntPtr LoadModelFromFile(string path_model, ModelParams params_);

        /// <summary>
        /// Load session file
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="path_session"></param>
        /// <param name="tokens_out"></param>
        /// <param name="n_token_capacity"></param>
        /// <param name="n_token_count_out"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_load_session_file", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool LoadSessionFile(SafeContextHandle ctx, string path_session, [Out] int[] tokens_out, ulong n_token_capacity, ulong* n_token_count_out);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_mlock_supported")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool MlockSupported();

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_mmap_supported")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool MmapSupported();

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_model_default_params")]
        [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
        public static extern ModelParams ModelDefaultParams();

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_n_ctx")]
        public static partial int NCtx(SafeContextHandle ctx);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_n_embd")]
        public static partial int NEmbd(SafeContextHandle ctx);

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// Return NULL on failure
        /// </summary>
        /// <param name="path_model"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_new_context_with_model")]
        [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time")]
        public static extern IntPtr NewContextWithModel(SafeModelHandle mdl, ContextParams params_);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_n_vocab")]
        public static partial int NVocab(SafeModelHandle ctx);

        /// <summary>
        /// Print system information
        /// </summary>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_print_system_info")]
        public static partial IntPtr PrintSystemInfo();

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_print_timings")]
        public static partial void PrintTimings(SafeContextHandle ctx);

        /// <summary>
        /// Removes all tokens that belong to the specified sequence and have positions in [startPos, endPos)
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_kv_cache_seq_rm")]
        public static partial void RemoveCacheTokens(SafeContextHandle handle, int sequenceId, int startPos, int endPos);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_reset_timings")]
        public static partial void ResetTimings(SafeContextHandle ctx);

        /// <summary>
        /// Save session file
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="path_session"></param>
        /// <param name="tokens"></param>
        /// <param name="n_token_count"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_save_session_file", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SaveSessionFile(SafeContextHandle ctx, string path_session, [In] int[] tokens, ulong n_token_count);

        /// <summary>
        /// Sets the current rng seed.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="seed"></param>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_set_rng_seed")]
        public static partial void SetRngSeed(SafeContextHandle ctx, int seed);

        /// <summary>
        /// Set the state reading from the specified address
        /// Returns the number of bytes read
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_set_state_data")]
        public static partial ulong SetStateData(SafeContextHandle ctx, [In] byte[] src);

        /// <summary>
        /// Adds relative position "delta" to all tokens that belong to the specified sequence and have positions in [startPos, endPos)
        /// If the KV cache is RoPEd, the KV data is updated accordingly.
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_kv_cache_seq_add")]
        public static partial void ShiftCacheTokens(SafeContextHandle handle, int sequenceId, int startPos, int endPos, int delta);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_token_bos")]
        public static partial int TokenBos();

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_token_eos")]
        public static partial int TokenEos();

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
        public static int Tokenize(SafeModelHandle ctx, string text, int[] tokens, int n_max_tokens, bool add_bos, bool special = false)
        {
            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(text);
            int result;

            unsafe
            {
                fixed (byte* pUtf8Bytes = utf8Bytes)
                {
                    result = TokenizeNative(ctx, (IntPtr)pUtf8Bytes, utf8Bytes.Length, tokens, n_max_tokens, add_bos, special);
                }
            }

            return result;
        }

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_tokenize")]
        public static partial int TokenizeNative(SafeModelHandle model,
            IntPtr text,
            int textLen,
            [Out] int[] tokens,
            int maxTokens,
            [MarshalAs(UnmanagedType.Bool)] bool addBos,
            [MarshalAs(UnmanagedType.Bool)] bool special);

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_token_nl")]
        public static partial int TokenNl();

        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_token_to_piece")]
        public static partial int TokenToPiece(SafeModelHandle model,
                                               int token,
                                               [Out] byte[] buf,
                                               int length,
                                               int ltrim,
                                               [MarshalAs(UnmanagedType.Bool)] bool special);

        /// <summary>
        /// Token Id -> String. Uses the vocabulary in the provided context
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns>Pointer to a string.</returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_token_get_text")]
        public static partial IntPtr TokenToStr(SafeContextHandle ctx, int token);
    }
}