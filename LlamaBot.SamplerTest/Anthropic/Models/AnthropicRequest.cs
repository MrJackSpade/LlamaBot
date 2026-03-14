using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlamaBot.SamplerTest.Anthropic.Models
{
    public class AnthropicContent
    {
        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string? Content { get; set; }

        // For tool_use
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string? Id { get; set; }

        [JsonProperty("input", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Input { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string? Text { get; set; }

        // For tool_result
        [JsonProperty("tool_use_id", NullValueHandling = NullValueHandling.Ignore)]
        public string? ToolUseId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "text";
    }

    public class AnthropicMessage
    {
        [JsonProperty("content")]
        public List<AnthropicContent> Content { get; set; } = [];

        [JsonProperty("role")]
        public string Role { get; set; } = "user";

        public static AnthropicMessage Assistant(string text)
        {
            return new AnthropicMessage
            {
                Role = "assistant",
                Content = [new AnthropicContent { Type = "text", Text = text }]
            };
        }

        public static AnthropicMessage AssistantWithToolUse(string? text, string toolId, string toolName, JObject toolInput)
        {
            List<AnthropicContent> content = new();

            if (!string.IsNullOrEmpty(text))
            {
                content.Add(new AnthropicContent { Type = "text", Text = text });
            }

            content.Add(new AnthropicContent
            {
                Type = "tool_use",
                Id = toolId,
                Name = toolName,
                Input = toolInput
            });

            return new AnthropicMessage
            {
                Role = "assistant",
                Content = content
            };
        }

        public static AnthropicMessage ToolResult(string toolId, string result)
        {
            return new AnthropicMessage
            {
                Role = "user",
                Content = [new AnthropicContent
                {
                    Type = "tool_result",
                    ToolUseId = toolId,
                    Content = result
                }]
            };
        }

        public static AnthropicMessage User(string text)
        {
            return new AnthropicMessage
            {
                Role = "user",
                Content = [new AnthropicContent { Type = "text", Text = text }]
            };
        }

        /// <summary>
        /// Gets the text content from all text blocks in this message.
        /// </summary>
        public string GetTextContent()
        {
            IEnumerable<AnthropicContent> textBlocks = Content.Where(c => c.Type == "text" && c.Text != null);
            return string.Join("\n", textBlocks.Select(c => c.Text));
        }
    }

    public class AnthropicRequest
    {
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 20000;

        [JsonProperty("messages")]
        public List<AnthropicMessage> Messages { get; set; } = [];

        [JsonProperty("model")]
        public string Model { get; set; } = "claude-sonnet-4-5-20250929";

        [JsonProperty("system")]
        public string? System { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 1;

        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public List<JObject>? Tools { get; set; }
    }
}