using LlamaNative.Chat.Models;

namespace LlamaBotAutomated
{
    public class MetaData
    {
        public Dictionary<ulong, AutoRespond> AutoResponds { get; set; } = [];

        public Dictionary<ulong, DateTime> ClearValues { get; set; } = [];
    }
}