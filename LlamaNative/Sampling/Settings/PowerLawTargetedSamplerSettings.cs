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

        public float TailDecay { get; set; } = 0.5f;

        /// <summary>
        /// Lower values = heavier tails, 2.0 = Cauchy
        /// </summary>
        public float TailHeaviness { get; set; } = 2.0f; 
    }
}