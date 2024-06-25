namespace LlamaNative.Samplers.Settings
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
        /// Maximum value before token is greedy sampled
        /// </summary>
        public Dictionary<int, float> MaxPs { get; set; } = [];

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
    }
}