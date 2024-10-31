using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;

namespace LlamaNative.Sampling.Samplers.Mirostat
{
    public class MirostatTwoSampler(MirostatSamplerSettings settings) : ITokenSelector
    {
        private readonly MirostatSamplerSettings _settings = settings;

        public int SampleNext(SampleContext sampleContext)
        {
            float mu = _settings.InitialMu;
            SamplingApi.Temperature(sampleContext.Candidates, _settings.Temperature);
            return SamplingApi.TokenMirostatV2(sampleContext.ContextHandle, sampleContext.Candidates, _settings.Tau, _settings.Eta, ref mu);
        }
    }
}