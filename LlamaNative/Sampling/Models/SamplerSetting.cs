using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LlamaNative.Sampling.Models
{
    public class SamplerSetting
    {
        public SamplerSetting(string type)
        {
            Type = type;
        }

        public SamplerSetting(string type, object settings)
        {
            Type = type;
            // Serialize the object to a JSON string
            string jsonString = JsonSerializer.Serialize(settings);

            // Parse the JSON string into a JsonObject
            Settings = JsonNode.Parse(jsonString).AsObject();
        }

        [JsonConstructor]
        private SamplerSetting()
        { }

        [JsonPropertyName("pop")]
        public string? Pop { get; set; }

        [JsonPropertyName("push")]
        public string? Push { get; set; }

        [JsonPropertyName("settings")]
        public JsonObject? Settings { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}