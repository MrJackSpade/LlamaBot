using LlamaNative.Chat.Models;

namespace LlamaBot
{
    public class MetaData
    {
        public Dictionary<ulong, AutoRespond> AutoResponds { get; set; } = [];

        public Dictionary<ulong, DateTime> ClearValues { get; set; } = [];
    }
}