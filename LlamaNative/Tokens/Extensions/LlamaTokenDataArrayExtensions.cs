using LlamaNative.Interop.Structs;
using LlamaNative.Logit.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Extensions
{
    public static class TokenDataArrayExtensions
    {
        public static float GetProbability(this TokenDataArray tokens, int tokenId)
        {
            Span<TokenData> span = tokens.Data.Span;
            int index = tokens.GetTokenIndex(tokenId);
            TokenData existing = span[index];
            return existing.logit;
        }

        public static void SetBias(this TokenDataArray tokens, int tokenId, float probability, LogitBiasType logitBiasType)
        {
            Span<TokenData> span = tokens.Data.Span;

            int index = tokens.GetTokenIndex(tokenId);

            TokenData existing = span[index];

            int mod = existing.logit > 0 ? 1 : -1;

            span[index] = logitBiasType switch
            {
                LogitBiasType.Additive => new TokenData()
                {
                    id = existing.id,
                    logit = existing.logit + probability,
                    p = existing.p + probability
                },
                LogitBiasType.Multiplicative => new TokenData()
                {
                    id = existing.id,
                    logit = existing.logit * probability * mod,
                    p = existing.p * probability * mod
                },
                _ => throw new NotImplementedException(),
            };

            tokens.Sorted = false;
        }

        public static void SetLogit(this TokenDataArray candidates, int tokenId, float logit)
        {
            Span<TokenData> span = candidates.Data.Span;
            int index = candidates.GetTokenIndex(tokenId);

            TokenData existing = span[index];
            span[index] = new TokenData()
            {
                id = existing.id,
                logit = logit,
                p = logit
            };
        }

        public static void SetLogitAtIndex(this TokenDataArray tokens, int index, float logit)
        {
            Span<TokenData> span = tokens.Data.Span;
            TokenData existing = span[index];
            span[index] = new TokenData()
            {
                id = existing.id,
                logit = logit,
                p = logit
            };

            tokens.Sorted = false;
        }

        public static void SetPenalty(this TokenDataArray tokens, int tokenId, float probability)
        {
            Span<TokenData> span = tokens.Data.Span;
            int index = tokens.GetTokenIndex(tokenId);

            TokenData existing = span[index];

            float newValue = existing.logit / probability;

            if (existing.logit <= 0)
            {
                newValue = existing.logit * probability;
            }

            span[index] = new TokenData()
            {
                id = existing.id,
                logit = newValue,
                p = newValue
            };

            tokens.Sorted = false;
        }

        public static void SetProbability(this TokenDataArray tokens, int tokenId, float probability)
        {
            Span<TokenData> span = tokens.Data.Span;
            int index = tokens.GetTokenIndex(tokenId);

            TokenData existing = span[index];
            span[index] = new TokenData()
            {
                id = existing.id,
                logit = probability,
                p = probability
            };

            tokens.Sorted = false;
        }

        public static void Update(this TokenDataArray tokens, IEnumerable<KeyValuePair<Token, float>> list)
        {
            foreach (KeyValuePair<Token, float> Token in list)
            {
                tokens.SetProbability(Token.Key.Id, Token.Value);
            }
        }

        private static int GetTokenIndex(this TokenDataArray tokens, int tokenId)
        {
            for (int i = 0; i < tokens.Data.Span.Length; i++)
            {
                if (tokens.Data.Span[i].id == tokenId)
                {
                    return i;
                }
            }

            throw new KeyNotFoundException();
        }
    }
}