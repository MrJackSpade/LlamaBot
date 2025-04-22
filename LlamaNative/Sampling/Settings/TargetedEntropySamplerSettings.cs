namespace LlamaNative.Sampling.Settings
{
    public class TargetedEntropySamplerSettings : BaseDynamicSamplerSettings
    {
        /// <summary>
        ///
        /// </summary>
        public float MaxTarget { get; set; } = 1f;

        /// <summary>
        ///
        /// </summary>
        public float MinTarget { get; set; } = 0f;

        /// <summary>
        /// Default .4
        /// </summary>
        public float Target { get; set; } = 0.4f;
    }
}