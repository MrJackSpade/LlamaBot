using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using System.Diagnostics;

namespace LlamaNative.Sampling.Samplers.Temperature
{
    /// <summary>
    /// A simple greedy sampler that always selects the highest probability token.
    /// Uses an empty settings object since no configuration is needed.
    /// </summary>
    public class GreedySampler : ITokenSelector<GreedySamplerSettings>
    {
        public int SampleNext(SampleContext sampleContext, GreedySamplerSettings settings)
        {
            int toReturn = SamplingApi.TokenGreedy(sampleContext.Candidates);

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] [G] ({toReturn}) [{NativeApi.TokenToPiece(sampleContext.ModelHandle, toReturn)}]");

            return toReturn;
        }
    }

    /// <summary>
    /// Empty settings class for GreedySampler (no configuration needed).
    /// </summary>
    public class GreedySamplerSettings
    {
    }
}