using Newtonsoft.Json;

namespace LlamaBot.SamplerTest
{
    internal class Configuration
    {
        [JsonProperty("message_count")]
        public int MessageCount { get; set; } = 100;

        [JsonProperty("output_directory")]
        public string OutputDirectory { get; set; } = "./Results";
    }
}