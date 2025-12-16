using System.Text.Json.Serialization;

namespace LlamaNative.Sampling.Settings
{
    public class PowerLawTargetedSamplerSettings : TargetedEntropySamplerSettings
    {
        public float DistributionWidth { get; set; } = 0.3f;

        /// <summary>
        /// Write token values to the output window
        /// </summary>
        public bool Log { get; set; }

        public float PeakLogitValue { get; set; } = 5.0f;

        public float TailDecay { get; set; } = 0.65f;

        /// <summary>
        /// Lower values = heavier tails, 2.0 = Cauchy
        /// </summary>
        public float TailHeaviness { get; set; } = 2.0f;

        // ============================================
        // Runtime state fields - not persisted to JSON
        // ============================================

        /// <summary>
        /// Total weight for the exponential decay average.
        /// Runtime state, not serialized.
        /// </summary>
        [JsonIgnore]
        public float TotalWeight { get; set; } = 0.0f;

        /// <summary>
        /// Running weighted sum of selected token probabilities for feedback loop.
        /// Runtime state, not serialized.
        /// </summary>
        [JsonIgnore]
        public float WeightedSum { get; set; } = 0.0f;
    }
}