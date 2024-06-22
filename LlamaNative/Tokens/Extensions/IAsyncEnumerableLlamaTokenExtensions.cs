using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Extensions
{
    public static class IAsyncEnumerableTokenExtensions
    {
        public static async Task<TokenCollection> ToCollection(this IAsyncEnumerable<Token> enumerable)
        {
            TokenCollection toReturn = new();

            await foreach (Token token in enumerable)
            {
                toReturn.Append(token);
            }

            return toReturn;
        }
    }
}