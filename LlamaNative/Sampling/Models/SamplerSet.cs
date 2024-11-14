using LlamaNative.Sampling.Interfaces;

namespace LlamaNative.Sampling.Models
{
    /// <summary>
    /// A stackable set of sampler configurations used to sample tokens.
    /// </summary>
    public class SamplerSet
    {
        public int Pop { get; set; } = -1;

        public int Push { get; set; } = -1;

        public IList<ISimpleSampler> SimpleSamplers { get; set; } = new List<ISimpleSampler>();

        public required ITokenSelector TokenSelector { get; set; }
    }
}