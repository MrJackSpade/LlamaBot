using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;

namespace LlamaNative.Sampling.Samplers.Temperature
{
    public class GreedySampler : ITokenSelector
    {
        public GreedySampler()
        {
        }

        public int SampleNext(SampleContext sampleContext)
        {
            return SamplingApi.TokenGreedy(sampleContext.Candidates);
        }
    }
}