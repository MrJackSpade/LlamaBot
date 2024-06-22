namespace LlamaNative.Samplers.Settings
{
    public class RepetitionBlockingSamplerSettings
    {
        /// <summary>
        /// Exclude from penalty
        /// </summary>
        public int MaxRepetitions { get; set; } = 0;
    }
}