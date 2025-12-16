using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;

namespace LlamaNative.Sampling.Samplers.Mirostat
{
    public class MirostatTwoSampler : ITokenSelector<MirostatSamplerSettings>
    {
        public int SampleNext(SampleContext sampleContext, MirostatSamplerSettings settings)
        {
            float mu = settings.InitialMu;
            SamplingApi.Temperature(sampleContext.Candidates, settings.Temperature);
            return SamplingApi.TokenMirostatV2(sampleContext.ContextHandle, sampleContext.Candidates, settings.Tau, settings.Eta, ref mu);
        }
    }
}