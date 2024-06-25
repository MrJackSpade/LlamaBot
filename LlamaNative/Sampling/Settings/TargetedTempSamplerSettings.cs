using LlamaNative.Samplers.Settings;

namespace Llama.Data.Models.Settings
{
    public class TargetedTempSamplerSettings : BaseDynamicSamplerSettings
    {
        /// <summary>
        ///
        /// </summary>
        public float MaxTarget { get; set; } = 1f;

        /// <summary>
        /// Min probability across all tokens
        /// </summary>
        public float MinP { get; set; } = 0.05f;

        /// <summary>
        ///
        /// </summary>
        public float MinTarget { get; set; } = 0f;

        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public float Scale { get; set; } = 1f;

        /// <summary>
        /// Default .4
        /// </summary>
        public float Target { get; set; } = 0.4f;

        /// <summary>
        /// Default 40
        /// </summary>
        public float Temperature { get; set; } = 1.2f;

        /// <summary>
        /// Default 0.95
        /// </summary>
        public float Tfs { get; set; } = 0.95f;
    }
}