using Newtonsoft.Json;

namespace LlamaBot
{
    internal class Configuration
    {
        [JsonProperty("channel_ids")]
        public List<ulong>? ChannelIds { get; set; }

        [JsonProperty("user_ids")]
        public List<ulong>? UserIds { get; set; }

        [JsonProperty("character")]
        public string Character { get; set; } = "LlamaBot";

        [JsonProperty("discord_token")]
        public string? DiscordToken { get; set; }
    }
}