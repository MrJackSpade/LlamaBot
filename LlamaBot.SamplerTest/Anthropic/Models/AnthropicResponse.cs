using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlamaBot.SamplerTest.Anthropic.Models
{
    public class AnthropicResponse
    {
        [JsonProperty("content")]
        public List<AnthropicResponseContent> Content { get; set; } = [];

        /// <summary>
        /// Checks if the response contains a tool use
        /// </summary>
        public bool HasToolUse => Content.Any(c => c.Type == "tool_use");

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;

        [JsonProperty("stop_reason")]
        public string? StopReason { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("usage")]
        public AnthropicUsage? Usage { get; set; }

        /// <summary>
        /// Gets the text content from the response, excluding tool use blocks
        /// </summary>
        public string GetTextContent()
        {
            IEnumerable<AnthropicResponseContent> textBlocks = Content.Where(c => c.Type == "text" && c.Text != null);
            return string.Join("\n", textBlocks.Select(c => c.Text));
        }

        /// <summary>
        /// Gets the first tool use block from the response, if any
        /// </summary>
        public AnthropicResponseContent? GetToolUse()
        {
            return Content.FirstOrDefault(c => c.Type == "tool_use");
        }
    }

    public class AnthropicResponseContent
    {
        // For tool_use
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string? Id { get; set; }

        [JsonProperty("input", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Input { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string? Text { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class AnthropicUsage
    {
        [JsonProperty("cache_creation_input_tokens")]
        public int CacheCreationInputTokens { get; set; }

        [JsonProperty("cache_read_input_tokens")]
        public int CacheReadInputTokens { get; set; }

        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }
}