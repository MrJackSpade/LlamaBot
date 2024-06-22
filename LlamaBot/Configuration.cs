using Newtonsoft.Json;

namespace LlamaBot
{
    internal class Configuration
    {
        [JsonProperty("discord_token")]
        public string? DiscordToken { get; set; }

        [JsonProperty("channel_ids")]
        public List<ulong> ChannelIds { get; set; } = [];

        [JsonProperty("character")]
        public string Character { get; set; } = "LlamaBot";
    }
}
