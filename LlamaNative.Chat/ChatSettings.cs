using LlamaNative.Chat.Models;
using LlamaNative.Interop.Settings;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Samplers.Temperature;

namespace LlamaNative.Chat
{
    public class ChatSettings
    {
        public string BeginText { get; set; } = "";

        public required string BotName { get; set; }

        public ChatTemplate ChatTemplate { get; set; } = new ChatTemplate();

        public LlamaContextSettings ContextSettings { get; set; } = new LlamaContextSettings();

        public required ModelSettings ModelSettings { get; set; }

        public List<ISimpleSampler> SimpleSamplers { get; set; } = [];

        public ITokenSelector TokenSelector { get; set; } = new TemperatureSampler(new TemperatureSamplerSettings());
    }
}