namespace LlamaNative.Chat.Models
{
    public struct ReadResponseSettings
    {
        public ChatSplitSettings? ChatSplitSettings { get; set; }

        public bool ContinueLast { get; set; }

        public bool PrependDefaultUser { get; set; }

        public string RespondingUser { get; set; }

        public string? ResponsePrepend { get; set; }

        /// <summary>
        /// Per-request sampler settings object. Type must match the active ITokenSelector's expected settings type.
        /// </summary>
        public object? SamplerSettings { get; set; }
    }
}