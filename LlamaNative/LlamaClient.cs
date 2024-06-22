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
            LlamaContextSettings contextSettings,
            params ISimpleSampler[] simpleSamplers)
        {
            return LoadContext(modelSettings, contextSettings, new TemperatureSampler(new TemperatureSamplerSettings()));
        }

        public static INativeContext LoadContext(
            ModelSettings modelSettings,
            LlamaContextSettings contextSettings,
            ITokenSelector tokenSelector,
            params ISimpleSampler[] simpleSamplers
        )
        {
            LlamaModel loadedModel = NativeApi.LoadModel(modelSettings);
            SafeLlamaContextHandle loadedContext = NativeApi.LoadContext(loadedModel.Handle, contextSettings);

            return new NativeContext(loadedContext,
                                     loadedModel.Handle,
                                     contextSettings,
                                     tokenSelector,
                                     simpleSamplers);
        }
    }
}
