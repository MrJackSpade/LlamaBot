namespace LlamaNative.Chat.Models
{
    public struct ReadResponseSettings
    {
        public ChatSplitSettings? ChatSplitSettings { get; set; }

        public bool ContinueLast { get; set; }

        public string RespondingUser { get; set; }

        public string? ResponsePrepend { get; set; }
    }
}