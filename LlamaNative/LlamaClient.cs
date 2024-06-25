using Llama.Core;
using LlamaNative.Apis;
using LlamaNative.Interfaces;
using LlamaNative.Interop.Settings;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Samplers.Temperature;

namespace LlamaNative
{
    public static class LlamaClient
    {
        public static INativeContext LoadContext(
            ModelSettings modelSettings,
            ContextSettings contextSettings,
            params ISimpleSampler[] simpleSamplers)
        {
            return LoadContext(modelSettings, contextSettings, new TemperatureSampler(new TemperatureSamplerSettings()), simpleSamplers);
        }

        public static INativeContext LoadContext(
            ModelSettings modelSettings,
            ContextSettings contextSettings,
            ITokenSelector tokenSelector,
            params ISimpleSampler[] simpleSamplers
        )
        {
            Model loadedModel = NativeApi.LoadModel(modelSettings);
            SafeContextHandle loadedContext = NativeApi.LoadContext(loadedModel.Handle, contextSettings, out ContextParams lparams);

            return new NativeContext(loadedContext,
                                     loadedModel.Handle,
                                     lparams,
                                     tokenSelector,
                                     simpleSamplers);
        }
    }
}