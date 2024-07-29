using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Extensions;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Sampling.Samplers.Repetition
{
    public class RepetitionBlockingSampler(RepetitionBlockingSamplerSettings settings) : ISimpleSampler
    {
        private readonly RepetitionBlockingSamplerSettings _settings = settings;

        public void SampleNext(SampleContext sampleContext)
        {
            TokenCollection sampleTokens = sampleContext.ContextTokens.Trim();

            LastTokens lastTokens = this.GetLastTokens(sampleTokens, TokenMask.Undefined, _settings.MaxRepetitions);

            if (lastTokens.Ids.Length == _settings.MaxRepetitions)
            {
                int[] distinctTokens = lastTokens.Ids.Distinct().ToArray();

                if (distinctTokens.Length == 1)
                {
                    sampleContext.Candidates.SetLogit(distinctTokens[0], float.NegativeInfinity);

                    //Has to be blocked for real real
                    sampleContext.OriginalCandidates.SetLogit(distinctTokens[0], float.NegativeInfinity);
                }
            }
        }
    }
}