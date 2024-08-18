using LlamaNative.Chat.Models;

namespace LlamaBot
{
    public class MetaData
    {
        public Dictionary<ulong, AutoRespond> AutoResponds { get; set; } = new Dictionary<ulong, AutoRespond>();

        public Dictionary<ulong, DateTime> ClearValues { get; set; } = new Dictionary<ulong, DateTime>();
    }
}