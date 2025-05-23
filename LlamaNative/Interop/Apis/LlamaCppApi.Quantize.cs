﻿using LlamaNative.Interop.Structs;
using System.Runtime.InteropServices;

namespace LlamaNative.Interop
{
    internal partial class LlamaCppApi
    {
        /// <summary>
        /// Returns 0 on success
        /// </summary>
        /// <param name="fname_inp"></param>
        /// <param name="fname_out"></param>
        /// <param name="ftype"></param>
        /// <param name="nthread">how many threads to use. If <=0, will use std::thread::hardware_concurrency(), else the number given</param>
        /// <remarks>not great API - very likely to change</remarks>
        /// <returns>Returns 0 on success</returns>
        [LibraryImport(LIBRARY_NAME, EntryPoint = "llama_model_quantize", StringMarshalling = StringMarshalling.Utf16)]
        public static partial int ModelQuantize(string fname_inp, string fname_out, LlamaFtype ftype, int nthread);
    }
}