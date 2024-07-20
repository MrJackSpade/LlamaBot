using LlamaNative.Chat.Models;
using LlamaNative.Interop.Settings;
using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers.Temperature;

namespace LlamaNative.Chat
{
    public class ChatSettings
    {
        public string BeginText { get; set; } = "";

        public string SystemPromptUser { get; set; } = "System";

        public string BotName { get; set; } = "LlamaBot";

        public ChatTemplate ChatTemplate { get; set; } = new ChatTemplate();

        public ContextSettings ContextSettings { get; set; } = new ContextSettings();

        public ModelSettings ModelSettings { get; set; }

        /// <summary>
        /// Forces the bot to vary its response by X number
        /// </summary>
        public int ResponseStartBlock { get; set; }

        public List<SamplerSetting> SimpleSamplers { get; set; } = [];

        public ChatSplitSettings? SplitSettings { get; set; }

        public SamplerSetting TokenSelector { get; set; } = new SamplerSetting(nameof(TemperatureSampler));
    }
}