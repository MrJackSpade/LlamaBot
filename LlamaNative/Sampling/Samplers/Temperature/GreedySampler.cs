using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Collections;
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

            Decode.Interfaces.KvCacheState cache = sampleContext.KvCache;

            TokenCollection tokens = new(cache.GetSequence(0)); //Only concerned about the primary sequence right now

            Debug.WriteLine($"[{tokens.Trim().Count:00000}] [G] ({toReturn}) [{NativeApi.TokenToPiece(sampleContext.ModelHandle, toReturn)}]");

            return toReturn;
        }
    }
}