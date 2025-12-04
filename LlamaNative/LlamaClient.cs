using LlamaNative.Apis;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Models;

namespace LlamaNative
{
    public static class LlamaClient
    {
        public static INativeContext LoadContext(
            Model loadedModel,
            ContextSettings contextSettings,
            List<SamplerSet> samplerSets
        )
        {
            SafeContextHandle loadedContext = NativeApi.LoadContext(loadedModel.Handle, contextSettings, out ContextParams lparams);

            return new NativeContext(loadedContext,
                                     loadedModel.Handle,
                                     lparams,
                                     samplerSets);
        }

        public static Model LoadModel(ModelSettings modelSettings)
        {
            return NativeApi.LoadModel(modelSettings);
        }
    }
}