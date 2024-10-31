namespace LlamaNative.Sampling.Settings
{
    public class TemperatureSamplerSettings : BaseDynamicSamplerSettings
    {
        public bool PreserveWords { get; set; } = true;

        public float Temperature { get; set; } = 1.0f;
    }
}