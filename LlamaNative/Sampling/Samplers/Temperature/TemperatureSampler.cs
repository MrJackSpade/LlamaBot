using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;

namespace LlamaNative.Sampling.Samplers.Temperature
{
    public class TemperatureSampler(TemperatureSamplerSettings temperatureSamplerSettings) : ISimpleSampler
    {
        public void SampleNext(SampleContext sampleContext)
        {
            for (ulong i = 0; i < sampleContext.Candidates.Size; i++)
            {
                float v = sampleContext.Candidates.Data.Span[(int)i].Logit;

                sampleContext.Candidates.Data.Span[(int)i].Logit = v / temperatureSamplerSettings.Temperature;
            }

            SamplingApi.SoftMax(sampleContext.Candidates, true);
        }
    }
}