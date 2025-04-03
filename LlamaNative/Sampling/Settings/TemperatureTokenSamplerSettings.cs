namespace LlamaNative.Sampling.Settings
{
    public class TemperatureTokenSamplerSettings : BaseDynamicSamplerSettings
    {
        public bool PreserveWords { get; set; } = true;

        public float Temperature { get; set; } = 1.0f;
    }
}