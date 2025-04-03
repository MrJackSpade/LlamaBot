namespace LlamaNative.Sampling.Settings
{
    public class GaussianTargetedSamplerSettings : TargetedEntropySamplerSettings
    {
        /// <summary>
        /// Default 0.1f
        /// </summary>
        public float DistributionWidth { get; set; } = 0.1f;
    }
}