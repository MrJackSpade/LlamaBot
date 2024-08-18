using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Interfaces;
using LlamaNative.Tokens.Models;
using System.Diagnostics.CodeAnalysis;

namespace LlamaNative.Sampling.Extensions
{
    public static class ISimpleSamplerExtensions
    {
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public static LastTokens GetLastTokens(this ISimpleSampler sampler, IReadOnlyTokenCollection collection, TokenMask tokenMask, int tryTake) => new(collection, tokenMask, tryTake, [], []);

        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public static LastTokens GetLastTokens(this ISimpleSampler sampler, IReadOnlyTokenCollection collection, TokenMask tokenMask, int tryTake, HashSet<int> include, HashSet<int> exclude) => new(collection, tokenMask, tryTake, include, exclude);
    }

    public class LastTokens
    {
        public LastTokens(IReadOnlyTokenCollection collection, TokenMask tokenMask, int tryTake, HashSet<int> include, HashSet<int> exclude)
        {
            int availableCount = collection.Trim().Ids.Count();

            if (tryTake == -1)
            {
                tryTake = availableCount;
            }

            int canTake = Math.Min(availableCount, tryTake);

            int skip = availableCount - canTake;

            IEnumerable<Token> availableEnumerable = collection.Trim().Skip(skip).Take(canTake);

            if (exclude.Count > 0)
            {
                availableEnumerable = availableEnumerable.Where(t => !exclude.Contains(t.Id));
            }
            else if (include.Count > 0)
            {
                availableEnumerable = availableEnumerable.Where(t => include.Contains(t.Id));
            }

            if (tokenMask != TokenMask.Undefined)
            {
                availableEnumerable = availableEnumerable.Where(t => t.Mask.HasFlag(tokenMask));
            }

            Ids = availableEnumerable.Select(t => t.Id).ToArray();
            Length = Ids.Length;
        }

        public int[] Ids { get; set; }

        public int Length { get; set; }
    }
}