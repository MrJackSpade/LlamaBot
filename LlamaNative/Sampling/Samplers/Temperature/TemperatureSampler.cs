using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Samplers.Mirostat;

namespace LlamaNative.Sampling.Samplers.Temperature
{
    public class TemperatureSampler(TemperatureSamplerSettings temperatureSamplerSettings) : BaseDynamicSampler(0, temperatureSamplerSettings), ITokenSelector
    {
        public int SampleNext(SampleContext sampleContext)
        {
            return this.SelectToken(sampleContext, out _);
        }
    }
}