using LlamaNative.Tokens.Models;

namespace LlamaNative.Samplers.Settings
{
    public class RepetitionSamplerSettings
    {
        /// <summary>
        /// Applies the penalty only to tokens with the given mask
        /// </summary>
        public TokenMask TokenMask { get; set; } = TokenMask.Bot;

        /// <summary>
        /// Exclude from penalty
        /// </summary>
        public int[] Exclude { get; set; } = [];

        /// <summary>
        /// A cumulative penalty applied for each instance of a token. Default 0
        /// </summary>
        public float FrequencyPenalty { get; set; } = 0.00f;

        /// <summary>
        /// If provided, sampler only includes contained tokens
        /// </summary>
        public int[] Include { get; set; } = [];

        /// <summary>
        /// A static value applied if a token is found any number of times within the range.
        /// </summary>
        public float PresencePenalty { get; set; } = 0.00f;

        /// <summary>
        /// Default 0
        /// </summary>
        public float RepeatPenalty { get; set; } = 0.00f;

        /// <summary>
        /// Default 64
        /// </summary>
        public int RepeatPenaltyWindow { get; set; } = 64;
    }
}