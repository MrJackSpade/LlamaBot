using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace LlamaNative.Sampling.Extensions
{
    public static class ISimpleSamplerExtensions
    {
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public static LastTokens GetLastTokens(this ISimpleSampler sampler, IReadOnlyTokenCollection collection, int tryTake) => new(collection, tryTake, [], []);

        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public static LastTokens GetLastTokens(this ISimpleSampler sampler, IReadOnlyTokenCollection collection, int tryTake, HashSet<int> include, HashSet<int> exclude) => new(collection, tryTake, include, exclude);
    }

    public class LastTokens
    {
        public LastTokens(IReadOnlyTokenCollection collection, int tryTake, HashSet<int> include, HashSet<int> exclude)
        {
            int availableCount = collection.Trim().Ids.Count();

            if (tryTake == -1)
            {
                tryTake = availableCount;
            }

            int canTake = Math.Min(availableCount, tryTake);

            int skip = availableCount - canTake;

            IEnumerable<int> availableEnumerable = collection.Trim().Ids.Skip(skip).Take(canTake);

            if (exclude.Count > 0)
            {
                availableEnumerable = availableEnumerable.Where(t => !exclude.Contains(t));
            }
            else if (include.Count > 0)
            {
                availableEnumerable = availableEnumerable.Where(include.Contains);
            }

            Ids = availableEnumerable.ToArray();
            Length = Ids.Length;
        }

        public int[] Ids { get; set; }

        public int Length { get; set; }
    }
}