using LlamaNative.Chat.Models;
using LlamaNative.Interop.Settings;

namespace LlamaNative.Chat
{
    public class ChatSettings
    {
        public string BeginText { get; set; } = "";

        public string BotName { get; set; } = "LlamaBot";

        public ChatTemplate ChatTemplate { get; set; } = new ChatTemplate();

        public bool ConditionalResponse { get; set; }

        public ContextSettings ContextSettings { get; set; } = new ContextSettings();

        public ModelSettings? DraftModelSettings { get; set; }

        public required ModelSettings ModelSettings { get; set; }

        /// <summary>
        /// Forces the bot to vary its response by X number
        /// </summary>
        public int ResponseStartBlock { get; set; }

        public List<SamplerSetConfiguration> SamplerSets { get; set; } = [];

        public ChatSplitSettings? SplitSettings { get; set; }

        public string SystemPromptUser { get; set; } = "System";
    }
}