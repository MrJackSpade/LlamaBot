using LlamaNative.Decode.Models;
using LlamaNative.Exceptions;
using LlamaNative.Interop;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Tokens.Models;
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

        public static int Eval(SafeContextHandle handle, int[] tokens, int length, uint evalPointer, int evalThreadCount)
        {
            Log(nameof(Eval), tokens, length, evalPointer, evalThreadCount);

            return LlamaCppApi.Eval(handle, tokens, length, (int)evalPointer, evalThreadCount);
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
            lparams.FlashAttn = contextSettings.FlashAttention;
            lparams.YarnOrigCtx = contextSettings.YarnOrigCtx ?? contextSettings.ContextSize ?? lparams.NCtx;

            if (lparams.TypeV != GgmlType.GGML_TYPE_F16 && !lparams.FlashAttn)
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
                int err = LlamaCppApi.ApplyLoraFromFile(ctx, contextSettings.LoraAdapter, string.IsNullOrEmpty(contextSettings.LoraBase) ? null : contextSettings.LoraBase, (int)contextSettings.ThreadCount);
                if (err != 0)
                {
                    throw new LlamaCppRuntimeError("Failed to apply lora adapter.");
                }
            }

            return ctx;
        }

        private static void FreeContext(nint handle)
        {
            LlamaCppApi.FreeContext(handle);
        }

        public static Model LoadModel(ModelSettings modelSettings)
        {
            ModelParams lparams = LlamaCppApi.ModelDefaultParams();

            lparams.NGpuLayers = modelSettings.GpuLayerCount;
            lparams.UseMmap = modelSettings.UseMemoryMap;
            lparams.UseMlock = modelSettings.UseMemoryLock;

            SetTensors(ref lparams, new float[16]);

            // Handle TensorBufferTypeOverrides
            IntPtr tensorOverridesPtr = IntPtr.Zero;
            if (!string.IsNullOrWhiteSpace(modelSettings.TensorBufferTypeOverrides))
            {
                // Create the tensor buffer type overrides using the C++ side function
                tensorOverridesPtr = LlamaCppApi.CreateTensorBufferTypeOverrides(modelSettings.TensorBufferTypeOverrides);
                lparams.TensorBufferTypeOverrides = tensorOverridesPtr;
            }

            if (!File.Exists(modelSettings.ModelPath))
            {
                throw new FileNotFoundException($"The model file does not exist: {modelSettings.ModelPath}");
            }

            nint model_ptr = LlamaCppApi.LoadModelFromFile(modelSettings.ModelPath, lparams);

            // Free the tensor buffer type overrides if we created them
            if (tensorOverridesPtr != IntPtr.Zero)
            {
                LlamaCppApi.FreeTensorBufferTypeOverrides(tensorOverridesPtr);
            }

            if (model_ptr == nint.Zero)
            {
                throw new LlamaCppRuntimeError($"Failed to load model {modelSettings.ModelPath}.");
            }

            SafeModelHandle handle = new(model_ptr, FreeModel);

            nint vocab_ptr = LlamaCppApi.GetVocab(handle);

            SafeVocabHandle vocab = new(vocab_ptr, (n) => { });

            int nvocab = LlamaCppApi.NVocab(vocab);

            return new(handle, vocab, nvocab);
        }

        private static void FreeModel(nint handle)
        {
            LlamaCppApi.FreeModel(handle);
        }

        public static int NVocab(SafeModelHandle handle)
        {
            nint vocab_ptr = LlamaCppApi.GetVocab(handle);
            SafeVocabHandle vocab = new(vocab_ptr, LlamaCppApi.FreeModel);
            return LlamaCppApi.NVocab(vocab);
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

        public static void RemoveCacheToken(SafeContextHandle handle, uint pos)
        {
            using var mem = GetMemoryHandle(handle);
            //TODO: This used to be -1
            LlamaCppApi.RemoveCacheTokens(mem, 0, (int)pos, (int)(pos + 1));
        }

        public static void RemoveCacheTokens(SafeContextHandle handle, uint startPos, uint endPos)
        {
            using var mem = GetMemoryHandle(handle);
            //TODO: This used to be -1
            LlamaCppApi.RemoveCacheTokens(mem, 0, (int)startPos, (int)endPos);
        }

        public static void ShiftCacheTokens(SafeContextHandle handle, uint sequenceId, uint startPos, uint endPos, int delta)
        {
            using var mem = GetMemoryHandle(handle);
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

        private class CellDefinition
        {
            public KvCell Cell { get; set; }

            public int Index { get; set; }
        }
    }
}