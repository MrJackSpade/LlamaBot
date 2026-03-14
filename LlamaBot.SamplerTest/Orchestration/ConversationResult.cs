using Newtonsoft.Json;

namespace LlamaBot.SamplerTest.Orchestration
{
    public class ConversationResult
    {
        [JsonProperty("character_system_prompt")]
        public string CharacterSystemPrompt { get; set; } = string.Empty;

        [JsonProperty("completed")]
        public bool Completed { get; set; }

        [JsonProperty("end_time")]
        public DateTime EndTime { get; set; }

        [JsonProperty("error_message", NullValueHandling = NullValueHandling.Ignore)]
        public string? ErrorMessage { get; set; }

        [JsonProperty("error_summary")]
        public Dictionary<string, int> ErrorSummary { get; set; } = [];

        [JsonProperty("exchanges")]
        public List<ConversationExchange> Exchanges { get; set; } = [];

        [JsonProperty("model_info")]
        public string ModelInfo { get; set; } = string.Empty;

        [JsonProperty("opening_message")]
        public string OpeningMessage { get; set; } = string.Empty;

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("total_exchanges")]
        public int TotalExchanges { get; set; }
    }
}