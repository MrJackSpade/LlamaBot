using LlamaNative.Chat.Models;

namespace LlamaBot.Extensions
{
    internal static class ListSamplerSetExtensions
    {
        public static SamplerSetConfiguration? GetDefault(this IEnumerable<SamplerSetConfiguration> samplerSets)
        {
            return samplerSets.Where(s => s.Push is null && s.Pop is null).SingleOrDefault();
        }
    }
}