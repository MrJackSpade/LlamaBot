using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Extensions
{
    public static class TokenCollectionExtensions
    {
        public static void Append(this TokenCollection target, IEnumerable<Token> source)
        {
            foreach (Token item in source)
            {
                target.Append(item);
            }
        }

        public static void AppendControl(this TokenCollection target, IEnumerable<int> source)
        {
            foreach (int item in source)
            {
                target.AppendControl(item);
            }
        }

        public static void AppendControl(this TokenCollection target, int id) => target.Append(new Token(id, null));

        public static bool Contains(this TokenCollection target, int tokenId)
        {
            foreach (Token token in target)
            {
                if (token.Id == tokenId)
                {
                    return true;
                }
            }

            return false;
        }

        public static TokenCollection From(this TokenCollection target, uint startIndex, Token startToken)
        {
            // Calculate the index to start from
            uint start = target.Count - startIndex;

            // Ensure the index is within valid bounds
            if (start < 0)
            {
                start = 0;
            }
            else if (start > target.Count)
            {
                start = target.Count;
            }

            // Find the first instance of startToken
            int index = target.FindIndex(start, token => startToken.Id == token.Id);

            // If startToken was not found, use the original start position
            if (index == -1)
            {
                index = (int)start;
            }

            // Copy from the found position (or the original start position if startToken was not found)
            return new TokenCollection(target.Skip(index));
        }

        public static TokenCollection Replace(this TokenCollection target, TokenCollection toFind, TokenCollection toReplace)
        {
            TokenCollection toReturn = new();

            for (int i = 0; i < target.Count; i++)
            {
                bool isMatch = false;

                if (i + toFind.Count <= target.Count)
                {
                    for (int ii = 0; ii < toFind.Count; ii++)
                    {
                        Token tokenA = toFind[ii];
                        Token tokenB = target[ii + i];

                        if (tokenA.Value == tokenB.Value)
                        {
                            isMatch = true;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    i += (int)toFind.Count;
                    foreach (Token tokenA in toReplace)
                    {
                        toReturn.Append(tokenA);
                    }
                }
                else
                {
                    toReturn.Append(target[i]);
                }
            }

            return toReturn;
        }

        public static void Slide(this TokenCollection target, IEnumerable<Token> source)
        {
            foreach (Token item in source)
            {
                target.Shift(item);
            }
        }

        public static IEnumerable<TokenCollection> Split(this TokenCollection target, int id)
        {
            TokenCollection toReturn = new();

            foreach (Token token in target)
            {
                if (token.Id == id)
                {
                    yield return toReturn;
                    toReturn = new TokenCollection();
                }
                else
                {
                    toReturn.Append(token);
                }
            }

            yield return toReturn;
        }

        public static IEnumerable<TokenCollection> Split(this TokenCollection target, string value, StringComparison stringComparison = StringComparison.Ordinal)
        {
            TokenCollection toReturn = new();

            foreach (Token token in target)
            {
                if (string.Equals(token.Value, value, stringComparison))
                {
                    yield return toReturn;
                    toReturn = new TokenCollection();
                }
                else
                {
                    toReturn.Append(token);
                }
            }

            yield return toReturn;
        }
    }
}