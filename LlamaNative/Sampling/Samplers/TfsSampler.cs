using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Interfaces;

namespace LlamaNative.Sampling.Samplers
{
    public class TfsSampler : ISimpleSampler
    {
        private readonly TfsSamplerSettings _settings;

        public TfsSampler(TfsSamplerSettings temperatureSamplerSettings)
        {
            _settings = temperatureSamplerSettings;
        }

        public void SampleNext(SampleContext sampleContext)
        {
            SamplingApi.TailFree(sampleContext.Candidates, _settings.Tfs, 1);
        }
    }
}