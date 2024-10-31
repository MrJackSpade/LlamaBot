using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;

namespace LlamaNative.Sampling.Samplers
{
    public class TfsSampler(TfsSamplerSettings temperatureSamplerSettings) : ISimpleSampler
    {
        private readonly TfsSamplerSettings _settings = temperatureSamplerSettings;

        public void SampleNext(SampleContext sampleContext)
        {
            SamplingApi.TailFree(sampleContext.Candidates, _settings.Tfs, 1);
        }
    }
}