using LlamaNative.Sampling.Interfaces;

namespace LlamaNative.Sampling.Models
{
    /// <summary>
    /// A stackable set of sampler configurations used to sample tokens.
    /// </summary>
    public class SamplerSet
    {
        public Dictionary<int, string> LogitBias { get; set; } = [];

        public int Pop { get; set; } = -1;

        public int Push { get; set; } = -1;

        public List<SamplerSetting> SimpleSamplers { get; set; } = [];

        public ITokenSelector TokenSelector { get; set; }

        public List<ISimpleSampler> TypedSimpleSamplers { get; set; } = [];
    }
}