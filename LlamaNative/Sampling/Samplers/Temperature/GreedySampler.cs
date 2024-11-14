using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using System.Diagnostics;

namespace LlamaNative.Sampling.Samplers.Temperature
{
    public class GreedySampler : ITokenSelector
    {
        public GreedySampler()
        {
        }

        public int SampleNext(SampleContext sampleContext)
        {
            int toReturn = SamplingApi.TokenGreedy(sampleContext.Candidates);

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] [G] ({toReturn}) [{NativeApi.TokenToPiece(sampleContext.ModelHandle, toReturn)}]");

            return toReturn;
        }
    }
}