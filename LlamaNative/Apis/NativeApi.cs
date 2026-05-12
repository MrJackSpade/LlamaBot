using LlamaNative.Decode.Models;
using LlamaNative.Exceptions;
using LlamaNative.Interop;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LlamaNative.Apis
{
    public static unsafe class NativeApi
    {
        static NativeApi()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static int Decode(SafeContextHandle handle, BatchDecode<int> batch, uint maxBatchSize)
        {
            //for logging
            uint toDecode = (uint)batch.Items.Count;
            uint decoded = 0;

            uint originalHighestPosition = 0;
            uint currentHighestPosition = 0;

            if (batch.Items.Count > 0) //Do we need this? Should we have a batch size zero?
            {
                //if we have to reprocess a token that is AFTER our current token, the logits will be all fucked up
                //when were done. We need to make SURE that the last token we process is what we original expected
                originalHighestPosition = batch.Items.Max(b => b.Position);
                currentHighestPosition = originalHighestPosition;
            }

            //grab the items
            List<BatchItem<int>> batchItems = [.. batch.Items];

            if (batch.Embeddings != null)
            {
                throw new NotImplementedException();
            }

            if (batch.Logits != null)
            {
                throw new NotImplementedException(batch.Logits.ToString());
            }

            do
            {
                BatchDecode<int> thisBatch = new();

                //Always process in order of position
                batchItems = [.. batchItems.OrderBy(b => b.Position)];

                //Our estimated batch size
                uint thisBatchSize = Math.Min(maxBatchSize, (uint)batchItems.Count);

                //Add the tokens to the batch
                for (int i = 0; i < thisBatchSize; i++)
                {
                    thisBatch.AddItem(batchItems[0]);
                    batchItems.RemoveAt(0);
                }

                thisBatchSize = (uint)thisBatch.Count;

                if (thisBatchSize > 1)
                {
                    //Log progress for sanity
                    Debug.WriteLine($"[{decoded + thisBatchSize}/{toDecode}]");
                }

                //Actually process this batch
                int result = ProcessBatch(handle, thisBatch);

                if (result != 0)
                {
                    return result;
                }

                decoded += thisBatchSize;
            } while (batchItems.Count > 0);

            return 0;
        }

        public static unsafe Span<float> GetLogits(SafeContextHandle ctx, int length)
        {
            float* logits = LlamaCppApi.GetLogitsIth(ctx, -1);

            if ((IntPtr)logits == IntPtr.Zero)
            {
                throw new LlamaCppRuntimeError("Failed to get logits.");
            }

            return new Span<float>(logits, length);
        }

        public static SafeContextHandle LoadContext(SafeModelHandle model, ContextSettings contextSettings, out ContextParams lparams)
        {
            lparams = LlamaCppApi.ContextDefaultParams();
            lparams.NCtx = contextSettings.ContextSize ?? lparams.NCtx;
            lparams.NBatch = contextSettings.BatchSize;
            lparams.NoPerf = true;
            lparams.TypeV = contextSettings.TypeV;
            lparams.TypeK = contextSettings.TypeK;
            lparams.Embeddings = contextSettings.GenerateEmbedding;
            lparams.RopeFreqBase = contextSettings.RopeFrequencyBase;
            lparams.RopeFreqScale = contextSettings.RopeFrequencyScaling;
            lparams.NThreadsBatch = (int)contextSettings.ThreadCount;
            lparams.NThreads = (int)contextSettings.ThreadCount;
            lparams.RopeScalingType = contextSettings.RopeScalingType;
            lparams.YarnBetaSlow = contextSettings.YarnBetaSlow;
            lparams.YarnBetaFast = contextSettings.YarnBetaFast;
            lparams.YarnAttnFactor = contextSettings.YarnAttnFactor;
            lparams.YarnExtFactor = contextSettings.YarnExtFactor;
            lparams.OffloadKQV = contextSettings.OffloadKQV;
            lparams.FlashAttentionType = contextSettings.FlashAttentionType;
            lparams.YarnOrigCtx = contextSettings.YarnOrigCtx ?? contextSettings.ContextSize ?? lparams.NCtx;

            Console.WriteLine($"Context Params: Context Size: {lparams.NCtx}, Batch Size: {lparams.NBatch}, Threads: {lparams.NThreads}, TypeK: {lparams.TypeK}, TypeV: {lparams.TypeV}, FlashAttentionType: {lparams.FlashAttentionType}, OffloadKQV: {lparams.OffloadKQV}");
            
            if (lparams.TypeV != GgmlType.GGML_TYPE_F16 && lparams.FlashAttentionType == FlashAttentionType.Disabled)
            {
                throw new ArgumentException("V cache quantization requires flash_attn");
            }

            nint ctx_ptr = LlamaCppApi.NewContextWithModel(model, lparams);

            if (ctx_ptr == nint.Zero)
            {
                throw new LlamaCppRuntimeError($"Failed to load context.");
            }

            SafeContextHandle ctx = new(ctx_ptr, FreeContext);

            if (!string.IsNullOrEmpty(contextSettings.LoraAdapter))
            {
                nint adapterPtr = LlamaCppApi.AdapterLoraInit(model, contextSettings.LoraAdapter);
                if (adapterPtr == nint.Zero)
                {
                    throw new LlamaCppRuntimeError("Failed to load lora adapter.");
                }

                if (LlamaCppApi.SetAdapterLora(ctx, adapterPtr, contextSettings.LoraScale) != 0)
                {
                    throw new LlamaCppRuntimeError("Failed to apply lora adapter.");
                }
            }

            return ctx;
        }

        /// <summary>
        /// Loads a model. When <see cref="ModelSettings.GpuLayerCount"/> is <see cref="ModelSettings.AutoGpuLayerCount"/>
        /// (-1) the number of GPU layers (and, for multi-GPU, the tensor split) is picked automatically to fit free
        /// VRAM; pass <paramref name="contextSettings"/> so that decision accounts for the context size that will be used.
        /// Any other value of <see cref="ModelSettings.GpuLayerCount"/> selects exactly that many layers, as before.
        /// </summary>
        public static Model LoadModel(ModelSettings modelSettings, ContextSettings? contextSettings = null)
        {
            ModelParams lparams = LlamaCppApi.ModelDefaultParams();

            lparams.UseMmap = modelSettings.UseMemoryMap;
            lparams.UseMlock = modelSettings.UseMemoryLock;
            lparams.VocabOnly = modelSettings.VocabOnly;

            bool autoFitGpuLayers = modelSettings.GpuLayerCount == ModelSettings.AutoGpuLayerCount;

            // Unmanaged buffers that must outlive the LoadModelFromFile call (the model reads them during load).
            IntPtr tensorOverridesPtr = IntPtr.Zero;
            IntPtr fitTensorSplitPtr = IntPtr.Zero;
            IntPtr fitTensorBuftOverridesPtr = IntPtr.Zero;
            IntPtr fitMarginsPtr = IntPtr.Zero;

            if (!File.Exists(modelSettings.ModelPath))
            {
                throw new FileNotFoundException($"The model file does not exist: {modelSettings.ModelPath}");
            }

            if (autoFitGpuLayers)
            {
                if (!string.IsNullOrWhiteSpace(modelSettings.TensorBufferTypeOverrides))
                {
                    throw new ArgumentException($"{nameof(ModelSettings.GpuLayerCount)} = {ModelSettings.AutoGpuLayerCount} (auto-fit) cannot be combined with {nameof(ModelSettings.TensorBufferTypeOverrides)}.");
                }

                // Leave lparams.NGpuLayers / TensorSplit / TensorBufferTypeOverrides at their library defaults so the
                // fitter is allowed to set them; it writes the chosen split / overrides into the buffers we pass in.
                FitGpuLayersToFreeMemory(modelSettings.ModelPath, ref lparams, contextSettings,
                    out fitTensorSplitPtr, out fitTensorBuftOverridesPtr, out fitMarginsPtr);
            }
            else
            {
                lparams.NGpuLayers = modelSettings.GpuLayerCount;

                SetTensors(ref lparams, new float[16]);

                // Handle TensorBufferTypeOverrides
                if (!string.IsNullOrWhiteSpace(modelSettings.TensorBufferTypeOverrides))
                {
                    // Create the tensor buffer type overrides using the C++ side function
                    tensorOverridesPtr = LlamaCppApi.CreateTensorBufferTypeOverrides(modelSettings.TensorBufferTypeOverrides);
                    lparams.TensorBufferTypeOverrides = tensorOverridesPtr;
                }
            }

            nint model_ptr = LlamaCppApi.LoadModelFromFile(modelSettings.ModelPath, lparams);

            // Free the tensor buffer type overrides if we created them
            if (tensorOverridesPtr != IntPtr.Zero)
            {
                LlamaCppApi.FreeTensorBufferTypeOverrides(tensorOverridesPtr);
            }

            if (fitTensorSplitPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(fitTensorSplitPtr);
            }

            if (fitTensorBuftOverridesPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(fitTensorBuftOverridesPtr);
            }

            if (fitMarginsPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(fitMarginsPtr);
            }

            if (model_ptr == nint.Zero)
            {
                throw new LlamaCppRuntimeError($"Failed to load model {modelSettings.ModelPath}.");
            }

            SafeModelHandle handle = new(model_ptr, FreeModel);

            // The fitter sizes the GPU layers for the model's trained context when no context size was declared; adopt that
            // size for the context that will actually be created so the two stay consistent.
            if (autoFitGpuLayers && contextSettings is not null && contextSettings.ContextSize is null)
            {
                int trainedContext = LlamaCppApi.ModelNCtxTrain(handle);
                if (trainedContext > 0)
                {
                    contextSettings.ContextSize = (uint)trainedContext;
                }
            }

            nint vocab_ptr = LlamaCppApi.GetVocab(handle);

            SafeVocabHandle vocab = new(vocab_ptr, (n) => { });

            int nvocab = LlamaCppApi.NVocab(vocab);

            return new(handle, vocab, nvocab);
        }

        public static int NVocab(SafeModelHandle handle)
        {
            nint vocab_ptr = LlamaCppApi.GetVocab(handle);
            // The vocab is owned by the model — it must not be freed separately.
            SafeVocabHandle vocab = new(vocab_ptr, (n) => { });
            return LlamaCppApi.NVocab(vocab);
        }

        public static void RemoveCacheToken(SafeContextHandle handle, uint pos)
        {
            using SafeMemoryHandle mem = GetMemoryHandle(handle);
            LlamaCppApi.RemoveCacheTokens(mem, -1, (int)pos, (int)(pos + 1));
        }

        public static void RemoveCacheTokens(SafeContextHandle handle, uint startPos, uint endPos)
        {
            using SafeMemoryHandle mem = GetMemoryHandle(handle);
            LlamaCppApi.RemoveCacheTokens(mem, -1, (int)startPos, (int)endPos);
        }

        public static void ShiftCacheTokens(SafeContextHandle handle, uint sequenceId, uint startPos, uint endPos, int delta)
        {
            using SafeMemoryHandle mem = GetMemoryHandle(handle);
            LlamaCppApi.ShiftCacheTokens(mem, (int)sequenceId, (int)startPos, (int)endPos, delta);
        }

        public static void Test()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static int[] Tokenize(SafeModelHandle ctx, string text, bool add_bos, bool useLegacy = true, bool parseSpecial = true)
        {
            int cnt = Encoding.Unicode.GetByteCount(text + 1);

            int[] res = new int[cnt + (add_bos ? 1 : 0)];

            int n = LlamaCppApi.Tokenize(ctx, text, res, res.Length, add_bos, parseSpecial);

            if (n < 0)
            {
                throw new LlamaCppRuntimeError("Error happened during tokenization. It's possibly caused by wrong encoding. Please try to specify the encoding.");
            }

            res = res.Take(n).ToArray();

            if (useLegacy && res[0] == 29871)
            {
                res = res.Skip(1).ToArray();
            }

            return res;
        }

        public static string TokenToPiece(this SafeModelHandle ctx, int token, bool special = true)
        {
            SafeVocabHandle vocab = new(LlamaCppApi.GetVocab(ctx), (n) => { });

            // Assuming a buffer size of 256, adjust as needed.
            byte[] buffer = new byte[256];

            int result;
            try
            {
                result = LlamaCppApi.TokenToPiece(vocab, token, buffer, buffer.Length, 0, special);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"An exception has occurred converting the token '{token}' to a string", e);
            }

            // Assuming a successful result is indicated by a non-negative value.
            // Adjust the condition based on the actual behavior of the C++ function.
            if (result < 0)
            {
                throw new InvalidOperationException($"Failed to convert token to piece. Error code: {result}");
            }

            string toReturn = System.Text.Encoding.UTF8.GetString(buffer, 0, result);

            return toReturn;
        }

        private static void FreeContext(nint handle)
        {
            LlamaCppApi.FreeContext(handle);
        }

        private static void FreeModel(nint handle)
        {
            LlamaCppApi.FreeModel(handle);
        }

        // Add helper to get memory handle
        private static SafeMemoryHandle GetMemoryHandle(SafeContextHandle context)
        {
            IntPtr memPtr = LlamaCppApi.GetMemory(context);
            if (memPtr == IntPtr.Zero)
            {
                throw new LlamaCppRuntimeError("Failed to get memory from context");
            }
            // Memory is owned by context, so we don't free it
            return new SafeMemoryHandle(memPtr, _ => { });
        }

        private static void Log(string method, params object[] args)
        {
            args ??= [];

            Debug.WriteLine($"{method}({string.Join(", ", args)})");
        }

        private static int ProcessBatch(SafeContextHandle handle, BatchDecode<int> thisBatch)
        {
            int[] tokens = new int[thisBatch.Items.Count];
            int[] pos = new int[thisBatch.Items.Count];
            int[] nseq = new int[thisBatch.Items.Count];

            for (int i = 0; i < thisBatch.Items.Count; i++)
            {
                tokens[i] = thisBatch.Items[i].Token;
                pos[i] = (int)thisBatch.Items[i].Position;
                nseq[i] = thisBatch.Items[i].SequenceIds.Length;
            }

            LlamaBatchNative nBatch = new()
            {
                NTokens = thisBatch.Items.Count,
                Token = Marshal.UnsafeAddrOfPinnedArrayElement(tokens, 0),
                Pos = Marshal.UnsafeAddrOfPinnedArrayElement(pos, 0),
                NSeqId = Marshal.UnsafeAddrOfPinnedArrayElement(nseq, 0),
                SeqId = Marshal.AllocHGlobal(nint.Size * thisBatch.Items.Count)
            };

            if (thisBatch.Logits != null)
            {
                nBatch.Logits = Marshal.UnsafeAddrOfPinnedArrayElement(thisBatch.Logits, 0);
            }

            if (thisBatch.Embeddings != null)
            {
                nBatch.Embd = Marshal.UnsafeAddrOfPinnedArrayElement(thisBatch.Embeddings, 0);
            }

            // Allocate and set the unmanaged memory for the sequence IDs
            for (int i = 0; i < thisBatch.Items.Count; i++)
            {
                int[] currentSeqIds = thisBatch.Items[i].SequenceIds;
                nint unmanagedArray = Marshal.AllocHGlobal(sizeof(int) * currentSeqIds.Length);

                // Copy the managed array to the unmanaged memory
                Marshal.Copy(currentSeqIds, 0, unmanagedArray, currentSeqIds.Length);

                // Set the pointer in the SeqId array
                Marshal.WriteIntPtr(nBatch.SeqId, i * nint.Size, unmanagedArray);
            }

            // Call the PInvoke method
            int result = LlamaCppApi.Decode(handle, nBatch);

            // Free the allocated memory
            Marshal.FreeHGlobal(nBatch.SeqId);

            return result;
        }

        private static void SetTensors(ref ModelParams param, float[] values)
        {
            // Populate your array.
            for (int i = 0; i < 16; i++)
            {
                values[i] = i;
            }

            // Allocate unmanaged memory for the array.
            nint tensorSplitPtr = Marshal.AllocHGlobal(16 * sizeof(float));

            // Copy the managed array to unmanaged memory.
            Marshal.Copy(values, 0, tensorSplitPtr, 16);

            // Now you can set the pointer in your structure.
            param.TensorSplit = tensorSplitPtr;
        }

        // ggml_log_level value used for the fitter's log output (INFO so the chosen layer count / fit breakdown is visible).
        private const int GgmlLogLevelInfo = 2;

        // Floor the fitter falls back to when no context size was declared and FitMinContextSize wasn't set — matches
        // llama.cpp's --fit-ctx default. (The fitter starts from the model's trained context and only shrinks toward this if needed.)
        private const uint LlamaDefaultFitMinContextSize = 4096;

        // Default per-device memory margin left free by the fitter, matching llama.cpp's CLI default of 1 GiB.
        private const ulong FitDefaultDeviceMarginBytes = 1024UL * 1024 * 1024;

        // The native llama_model_tensor_buft_override struct is two pointers: { const char* pattern; ggml_backend_buffer_type_t buft; }.
        private static int TensorBuftOverrideSize => 2 * IntPtr.Size;

        /// <summary>
        /// Asks llama.cpp to pick <see cref="ModelParams.NGpuLayers"/> (and, for multi-GPU, the tensor split and any
        /// overflow buffer-type overrides) so the model fits in free device memory, mutating <paramref name="lparams"/>
        /// in place.
        /// <para>
        /// Context-size handling: the GPU layer count is sized for <see cref="ContextSettings.ContextSize"/> (or, when that
        /// is unset, the model's trained context length) and that context size is <b>not</b> reduced — unless
        /// <see cref="ContextSettings.FitMinContextSize"/> is set, in which case, only if the model would not otherwise fit,
        /// the context is allowed to shrink down to that floor (and never above the declared <see cref="ContextSettings.ContextSize"/>).
        /// If the fitter ends up choosing the context size itself, <paramref name="contextSettings"/> is updated so the
        /// subsequently-created context matches what was fitted for.
        /// </para>
        /// The returned pointers own unmanaged buffers that <see cref="ModelParams"/> now references and that the caller
        /// must free once the model has been loaded.
        /// </summary>
        private static void FitGpuLayersToFreeMemory(
            string modelPath,
            ref ModelParams lparams,
            ContextSettings? contextSettings,
            out IntPtr tensorSplitPtr,
            out IntPtr tensorBuftOverridesPtr,
            out IntPtr marginsPtr)
        {
            int maxDevices = (int)LlamaCppApi.MaxDevices();
            int maxTensorBuftOverrides = (int)LlamaCppApi.MaxTensorBuftOverrides();

            int tensorSplitBytes = maxDevices * sizeof(float);
            int tensorBuftOverridesBytes = maxTensorBuftOverrides * TensorBuftOverrideSize;
            int marginsBytes = maxDevices * IntPtr.Size;

            IntPtr tsBuf = Marshal.AllocHGlobal(tensorSplitBytes);
            IntPtr tboBuf = Marshal.AllocHGlobal(tensorBuftOverridesBytes);
            IntPtr marginBuf = Marshal.AllocHGlobal(marginsBytes);

            // Values mparams must be reset to before each fit attempt: the fitter rejects model params it considers
            // "already set by the user", so on a retry we have to undo whatever a previous attempt assigned.
            ModelParams mparams = lparams;
            int defaultNGpuLayers = mparams.NGpuLayers;
            IntPtr defaultTensorSplit = mparams.TensorSplit;
            IntPtr defaultTensorBuftOverrides = mparams.TensorBufferTypeOverrides;

            try
            {
                new Span<nuint>((void*)marginBuf, maxDevices).Fill((nuint)FitDefaultDeviceMarginBytes);

                uint? declaredContextSize = contextSettings?.ContextSize is uint c && c != 0 ? c : null;
                uint? minContextSize = contextSettings?.FitMinContextSize is uint m && m != 0 ? m : null;

                ContextParams cparams = LlamaCppApi.ContextDefaultParams();

                // Mirror the memory-relevant context settings so the fit reflects the context that will actually be created.
                if (contextSettings is not null)
                {
                    cparams.NBatch = contextSettings.BatchSize;
                    cparams.TypeK = contextSettings.TypeK;
                    cparams.TypeV = contextSettings.TypeV;
                    cparams.FlashAttentionType = contextSettings.FlashAttentionType;
                    cparams.OffloadKQV = contextSettings.OffloadKQV;
                }

                LlamaCppApi.ParamsFitStatus RunFit(uint nCtx, uint nCtxMin)
                {
                    // reset everything the fitter is allowed to set, plus the scratch buffers it writes into
                    mparams.NGpuLayers = defaultNGpuLayers;
                    mparams.TensorSplit = defaultTensorSplit;
                    mparams.TensorBufferTypeOverrides = defaultTensorBuftOverrides;
                    new Span<byte>((void*)tsBuf, tensorSplitBytes).Clear();
                    new Span<byte>((void*)tboBuf, tensorBuftOverridesBytes).Clear();
                    cparams.NCtx = nCtx;

                    int code = LlamaCppApi.ParamsFit(
                        modelPath, ref mparams, ref cparams,
                        tsBuf, tboBuf, marginBuf,
                        nCtxMin, GgmlLogLevelInfo);

                    if ((LlamaCppApi.ParamsFitStatus)code == LlamaCppApi.ParamsFitStatus.Error)
                    {
                        throw new LlamaCppRuntimeError($"Failed to auto-fit GPU layers for {modelPath} (fit error).");
                    }

                    return (LlamaCppApi.ParamsFitStatus)code;
                }

                bool allowShrink = minContextSize is uint floor && (declaredContextSize is not uint declaredCap || floor < declaredCap);

                LlamaCppApi.ParamsFitStatus status;
                uint chosenContextSize;
                bool fitterChoseContext;

                if (declaredContextSize is uint declared)
                {
                    // Pin the declared context size (a non-zero n_ctx tells the fitter never to shrink it); only fall back
                    // to a smaller context if FitMinContextSize explicitly allowed it and the model would not otherwise fit.
                    status = RunFit(declared, declared);
                    if (status == LlamaCppApi.ParamsFitStatus.Failure && allowShrink)
                    {
                        status = RunFit(0, minContextSize!.Value);
                        // the fitter only writes n_ctx when it had to reduce it; cap it to the declared size either way
                        chosenContextSize = cparams.NCtx != 0 ? Math.Min(cparams.NCtx, declared) : declared;
                        fitterChoseContext = true;
                    }
                    else
                    {
                        chosenContextSize = declared;
                        fitterChoseContext = false;
                    }
                }
                else
                {
                    // No context size was declared: let the fitter use the model's trained context, shrinking only down to
                    // FitMinContextSize (or the llama default floor when that wasn't set).
                    status = RunFit(0, minContextSize ?? LlamaDefaultFitMinContextSize);
                    // n_ctx == 0 here means "fits at the model's trained context"; the caller resolves that once the model is loaded.
                    chosenContextSize = cparams.NCtx;
                    fitterChoseContext = true;
                }

                // Only adopt a concrete context size; n_ctx == 0 stays null and is resolved from the model's trained context post-load.
                if (fitterChoseContext && chosenContextSize != 0 && contextSettings is not null)
                {
                    contextSettings.ContextSize = chosenContextSize;
                }

                string contextSizeText = chosenContextSize != 0 ? chosenContextSize.ToString() : "the model's trained context";
                // A negative n_gpu_layers means the fitter left it at its default — i.e. the whole model fits and every layer is offloaded.
                string layersText = mparams.NGpuLayers < 0 ? "all layers (fits entirely in device memory)" : $"{mparams.NGpuLayers} layers";
                if (status == LlamaCppApi.ParamsFitStatus.Failure)
                {
                    Console.WriteLine($"Warning: could not fully fit '{modelPath}' to free device memory; proceeding with {layersText} on GPU, context size {contextSizeText}.");
                }
                else
                {
                    Console.WriteLine($"Auto-fit selected {layersText} on GPU for context size {contextSizeText}.");
                }

                lparams = mparams;
                tensorSplitPtr = tsBuf;
                tensorBuftOverridesPtr = tboBuf;
                marginsPtr = marginBuf;
            }
            catch
            {
                Marshal.FreeHGlobal(tsBuf);
                Marshal.FreeHGlobal(tboBuf);
                Marshal.FreeHGlobal(marginBuf);
                tensorSplitPtr = IntPtr.Zero;
                tensorBuftOverridesPtr = IntPtr.Zero;
                marginsPtr = IntPtr.Zero;
                throw;
            }
        }
    }
}