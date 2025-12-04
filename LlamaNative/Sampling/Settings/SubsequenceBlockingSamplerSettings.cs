namespace LlamaNative.Sampling.Settings
{
    public class SubsequenceBlockingSamplerSettings
    {
        /// <summary>
        /// Exclude from penalty
        /// </summary>
        public int[] Exclude { get; set; } = [];

        public int ResponseStartBlock { get; set; }

        public string[] SubSequences { get; set; } = [];
    }
}