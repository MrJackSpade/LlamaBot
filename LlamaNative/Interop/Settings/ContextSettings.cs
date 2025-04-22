using LlamaNative.Interop.Structs;

namespace LlamaNative.Interop.Settings
{
    public record ContextSettings
    {
        /// <summary>
        /// Default CPU Count - 2
        /// </summary>
        public uint EvalThreadCount { get; set; } = (uint)(Environment.ProcessorCount / 2);

        /// <summary>
        /// Default 512
        /// </summary>
        public uint BatchSize { get; set; } = 512;

        /// <summary>
        /// Default false
        /// </summary>
        public bool FlashAttention { get; set; }

        /// <summary>
        /// Default true
        /// </summary>
        public bool OffloadKQV { get; set; } = true;

        /// <summary>
        /// Default 4096
        /// </summary>
        public uint? ContextSize { get; set; }

        /// <summary>
        /// Default empty string
        /// </summary>
        public string LoraAdapter { get; set; } = string.Empty;

        /// <summary>
        /// Default empty string
        /// </summary>
        public string LoraBase { get; set; } = string.Empty;

        /// <summary>
        /// Default GGML_TYPE_F16
        /// </summary>
        public GgmlType TypeK { get; set; } = GgmlType.GGML_TYPE_F16;

        /// <summary>
        /// Default GGML_TYPE_F16
        /// </summary>
        public GgmlType TypeV { get; set; } = GgmlType.GGML_TYPE_F16;

        /// <summary>
        /// Default false
        /// </summary>
        public bool Perplexity { get; set; }

        /// <summary>
        /// Default 10_000 (Model Set)
        /// </summary>
        public float RopeFrequencyBase { get; set; } = 0;

        /// <summary>
        /// Default 1.0
        /// </summary>
        public float RopeFrequencyScaling { get; set; } = 0;

        /// <summary>
        /// Default CPU Count - 2
        /// </summary>
        public uint ThreadCount { get; set; } = (uint)Math.Max(Environment.ProcessorCount / 2, 1);

        /// <summary>
        /// Default false
        /// </summary>
        public bool GenerateEmbedding { get; set; }

        /// <summary>
        /// Default RopeScalingType.Unspecified
        /// </summary>
        public RopeScalingType RopeScalingType { get; set; } = RopeScalingType.Unspecified;

        /// <summary>
        /// Default -1.0f
        /// </summary>
        public float YarnExtFactor { get; set; } = -1.0f;

        /// <summary>
        /// Default 1.0f
        /// </summary>
        public float YarnAttnFactor { get; set; } = 1.0f;

        /// <summary>
        /// Default 32.0f
        /// </summary>
        public float YarnBetaFast { get; set; } = 32.0f;

        /// <summary>
        /// Default 1.0f
        /// </summary>
        public float YarnBetaSlow { get; set; } = 1.0f;

        /// <summary>
        /// Default null
        /// </summary>
        public uint? YarnOrigCtx { get; set; }
    }
}