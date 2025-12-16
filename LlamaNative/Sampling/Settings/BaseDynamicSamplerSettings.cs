using LlamaNative.Interop.Structs;
using System.Text.Json.Serialization;

namespace LlamaNative.Sampling.Settings
{
    public class BaseDynamicSamplerSettings
    {
        /// <summary>
        /// If true, Mirostat will use preserved words to adjust the temperature.
        /// If false, only words selected by temperature will be used to guide
        /// algorithm
        /// </summary>
        public bool FactorPreservedWords { get; set; } = false;

        /// <summary>
        /// Exclude specific tokens from greedy sampling
        /// </summary>
        public int[] GreedyExclude { get; set; } = [];

        /// <summary>
        /// Include specific tokens in greedy sampling
        /// </summary>
        public int[] GreedyInclude { get; set; } = [];

        /// <summary>
        /// Cache of token IDs that are known to be word continuations.
        /// Runtime state, not serialized.
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, bool> IsWordsCache { get; } = [];

        /// <summary>
        /// Maximum value before token is greedy sampled
        /// </summary>
        public Dictionary<int, float> MaxPs { get; set; } = [];

        /// <summary>
        /// Min probability across all tokens
        /// </summary>
        public float MinP { get; set; } = 0.05f;

        /// <summary>
        /// Minimum value that will allow a return for the EOS token
        /// </summary>
        public Dictionary<int, float> MinPs { get; set; } = [];

        /// <summary>
        /// The certainty at which word continuations are greedy sampled
        /// </summary>
        public float PreserveWordMaxP { get; set; } = .8f;

        /// <summary>
        /// The min probability for any word continuation
        /// </summary>
        public float PreserveWordMinP { get; set; } = .2f;

        /// <summary>
        /// Size of the token queue for dynamic adjustment
        /// </summary>
        public virtual int QueueSize { get; set; } = 10;

        // ============================================
        // Runtime state fields - not persisted to JSON
        // ============================================
        /// <summary>
        /// History of recently selected tokens for dynamic adjustment.
        /// Runtime state, not serialized.
        /// </summary>
        [JsonIgnore]
        public Queue<TokenData> SelectionHistory { get; } = new();
    }
}