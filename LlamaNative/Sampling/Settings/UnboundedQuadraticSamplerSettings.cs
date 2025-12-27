using System.Text.Json.Serialization;

namespace LlamaNative.Sampling.Settings
{
    /// <summary>
    /// Settings for the Unbounded Quadratic Sampler.
    /// Uses an adaptive sharpness formula: quadratic near target, linear far away.
    /// Logit = PEAK - SHARPNESS * dist² / (1 + |dist|)
    /// Unlike Power Law, this has unbounded negative logits for proper exponential suppression after softmax.
    /// </summary>
    public class UnboundedQuadraticSamplerSettings : TargetedEntropySamplerSettings
    {
        /// <summary>
        /// Width of the distribution. Smaller values create sharper peaks.
        /// Default: 0.3
        /// </summary>
        public float DistributionWidth { get; set; } = 0.3f;

        /// <summary>
        /// Write token values to the output window for debugging.
        /// </summary>
        public bool Log { get; set; }

        /// <summary>
        /// Peak logit value at the target probability.
        /// Default: 5.0
        /// </summary>
        public float PeakLogitValue { get; set; } = 5.0f;

        /// <summary>
        /// Sharpness of the distribution. Higher values create steeper falloff.
        /// Can be higher than Power Law since the tail is gentler (linear vs quadratic).
        /// Default: 4.0
        /// </summary>
        public float Sharpness { get; set; } = 4.0f;

        /// <summary>
        /// Decay factor for the exponential weighted average of selected probabilities.
        /// Default: 0.65
        /// </summary>
        public float TailDecay { get; set; } = 0.65f;

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
