using LlamaNative.Interop.Structs;
using System.Runtime.InteropServices;

namespace LlamaNative.Interop
{
    /// <summary>
    /// Bindings for the llama.cpp automatic parameter-fitting feature (the <c>--fit</c> / <c>-ngl auto</c>
    /// behaviour). Unlike the rest of <see cref="LlamaCppApi"/> these live in the <c>llama-common</c>
    /// library rather than core <c>llama</c>: <c>llama_params_fit</c> is a thin <c>extern "C"</c> wrapper
    /// (added in the MrJackSpade/llama.cpp fork) around <c>common_fit_params()</c>.
    /// </summary>
    internal unsafe partial class LlamaCppApi
    {
#if WINDOWS
        private const string COMMON_LIBRARY_NAME = "llama-common";
#else
        private const string COMMON_LIBRARY_NAME = "libllama-common.so";
#endif

        /// <summary>Status returned by <see cref="ParamsFit"/> (mirrors <c>common_params_fit_status</c>).</summary>
        public enum ParamsFitStatus
        {
            /// <summary>Found allocations that are projected to fit.</summary>
            Success = 0,

            /// <summary>Could not find allocations that are projected to fit; parameters may be partially adjusted.</summary>
            Failure = 1,

            /// <summary>A hard error occurred (e.g. no model could be found at the specified path).</summary>
            Error = 2,
        }

        /// <summary>The model's assumed (trained) context length, i.e. the size <c>n_ctx == 0</c> resolves to at context creation.</summary>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_model_n_ctx_train")]
        public static extern int ModelNCtxTrain(SafeModelHandle model);

        /// <summary>Maximum number of devices supported (i.e. the required length of a <c>tensor_split</c> buffer).</summary>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_max_devices")]
        public static extern nuint MaxDevices();

        /// <summary>Maximum number of tensor buffer-type overrides supported (i.e. the required length of a <c>tensor_buft_overrides</c> buffer).</summary>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_max_tensor_buft_overrides")]
        public static extern nuint MaxTensorBuftOverrides();

        /// <summary>
        /// Adjusts <paramref name="mparams"/> and <paramref name="cparams"/> in place so the model is projected to
        /// fit in free device memory (the llama.cpp "fit params" feature). Only fields still left at their library
        /// defaults are modified — anything the caller has explicitly set is preserved — with the exception of the
        /// context size, which is only changed if it is 0.
        /// </summary>
        /// <param name="pathModel">Path to the GGUF model file (it is opened with <c>no_alloc</c> to simulate allocations).</param>
        /// <param name="tensorSplit">Writable buffer of at least <see cref="MaxDevices"/> floats; receives the chosen split. Must outlive the subsequent model load.</param>
        /// <param name="tensorBuftOverrides">Writable buffer of at least <see cref="MaxTensorBuftOverrides"/> <c>llama_model_tensor_buft_override</c> entries (two pointers / 16 bytes each on x64), zero-initialised; receives any overflow overrides. Must outlive the subsequent model load.</param>
        /// <param name="margins">Buffer of at least <see cref="MaxDevices"/> <c>size_t</c> entries giving the bytes to keep free per device, or <see cref="IntPtr.Zero"/> to use the 1 GiB default.</param>
        /// <param name="nCtxMin">Minimum context size the fitter may fall back to when it needs to reduce memory use.</param>
        /// <param name="logLevel">A <c>ggml_log_level</c> value; messages below it are routed to the debug log only.</param>
        /// <returns>The fit status code (see <see cref="ParamsFitStatus"/>).</returns>
        [DllImport(COMMON_LIBRARY_NAME, EntryPoint = "llama_params_fit")]
        public static extern int ParamsFit(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pathModel,
            ref ModelParams mparams,
            ref ContextParams cparams,
            IntPtr tensorSplit,
            IntPtr tensorBuftOverrides,
            IntPtr margins,
            uint nCtxMin,
            int logLevel);
    }
}
