using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Samplers.Mirostat;
using LlamaNative.Sampling.Settings;

namespace LlamaNative.Sampling.Samplers.Temperature
{
    public class TemperatureSampler(TemperatureSamplerSettings temperatureSamplerSettings) : BaseDynamicSampler<TemperatureSamplerSettings>(0, temperatureSamplerSettings), ITokenSelector
    {
        public int SampleNext(SampleContext sampleContext)
        {
            return this.SelectToken(sampleContext, _settings.Temperature <= 0, out _);
        }
    }
}