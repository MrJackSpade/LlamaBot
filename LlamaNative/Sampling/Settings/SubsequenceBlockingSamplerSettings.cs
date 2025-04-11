namespace LlamaNative.Sampling.Settings
{
    public class SubsequenceBlockingSamplerSettings
    {
        public int ResponseStartBlock { get; set; }

        public string SubSequence { get; set; }

        /// <summary>
        /// Exclude from penalty
        /// </summary>
        public int[] Exclude { get; set; } = [];
    }
}