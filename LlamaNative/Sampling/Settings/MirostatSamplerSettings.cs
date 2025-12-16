using System.Text.Json.Serialization;

namespace LlamaNative.Sampling.Settings
{
    public class MirostatSamplerSettings
    {
        /// <summary>
        /// 100
        /// </summary>
        public readonly int M = 100;

        /// <summary>
        /// Default 0.1
        /// </summary>
        public float Eta { get; set; } = 0.10f;

        /// <summary>
        /// Tau * 2
        /// </summary>
        public float InitialMu => Tau * 2.0f;

        /// <summary>
        /// Cache of token IDs that are known to be word continuations.
        /// Runtime state, not serialized.
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, bool> IsWordsCache { get; } = [];

        /// <summary>
        /// Current mu value for Mirostat algorithm.
        /// Runtime state, not serialized.
        /// </summary>
        [JsonIgnore]
        public float Mu { get; set; }

        // ============================================
        // Runtime state fields - not persisted to JSON
        // ============================================
        /// <summary>
        /// Whether Mu has been initialized from InitialMu.
        /// Runtime state, not serialized.
        /// </summary>
        [JsonIgnore]
        public bool MuInitialized { get; set; } = false;

        /// <summary>
        /// If true, Mirostat will only use TOPK sampling for new words
        /// </summary>
        public bool PreserveWords { get; set; } = true;

        /// <summary>
        /// Default 5
        /// </summary>
        public float Tau { get; set; } = 5.00f;

        /// <summary>
        /// Default 0.85
        /// </summary>
        public float Temperature { get; set; } = 0.85f;
    }
}