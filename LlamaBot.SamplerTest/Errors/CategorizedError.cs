using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlamaBot.SamplerTest.Errors
{
    public class CategorizedError
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("category")]
        public ErrorCategory Category { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("quote")]
        public string? Quote { get; set; }
    }
}