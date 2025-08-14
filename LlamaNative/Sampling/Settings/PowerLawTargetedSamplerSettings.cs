namespace LlamaNative.Sampling.Settings
{
    public class PowerLawTargetedSamplerSettings : TargetedEntropySamplerSettings
    {
        public float DistributionWidth { get; set; } = 0.2f;

        public float TailHeaviness { get; set; } = 3.0f; // Lower values = heavier tails, 2.0 = Cauchy

        public float PeakLogitValue { get; set; } = 3.0f; // Peak value for the bell curve
        
        /// <summary>
        /// Write token values to the output window
        /// </summary>
        public bool Log { get; set; }
    }
}