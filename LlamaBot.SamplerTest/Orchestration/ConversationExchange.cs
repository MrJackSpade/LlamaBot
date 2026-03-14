using LlamaBot.SamplerTest.Errors;
using Newtonsoft.Json;

namespace LlamaBot.SamplerTest.Orchestration
{
    public class ConversationExchange
    {
        [JsonProperty("claude_message")]
        public string ClaudeMessage { get; set; } = string.Empty;

        [JsonProperty("errors")]
        public List<CategorizedError> Errors { get; set; } = [];

        [JsonProperty("exchange_number")]
        public int ExchangeNumber { get; set; }

        [JsonProperty("model_response")]
        public string ModelResponse { get; set; } = string.Empty;

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}