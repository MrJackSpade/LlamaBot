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
            return existing.Logit;
        }

        public static void SetBias(this TokenDataArray tokens, int tokenId, float probability, LogitBiasType logitBiasType)
        {
            Span<TokenData> span = tokens.Data.Span;

            int index = tokens.GetTokenIndex(tokenId);

            TokenData existing = span[index];

            int mod = existing.Logit > 0 ? 1 : -1;

            span[index] = logitBiasType switch
            {
                LogitBiasType.Additive => new TokenData()
                {
                    Id = existing.Id,
                    Logit = existing.Logit + probability,
                    P = existing.P + probability
                },
                LogitBiasType.Multiplicative => new TokenData()
                {
                    Id = existing.Id,
                    Logit = existing.Logit * probability * mod,
                    P = existing.P * probability * mod
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
                Id = existing.Id,
                Logit = logit,
                P = logit
            };

            candidates.Sorted = false;
        }

        public static void SetLogitAtIndex(this TokenDataArray tokens, int index, float logit)
        {
            Span<TokenData> span = tokens.Data.Span;
            TokenData existing = span[index];
            span[index] = new TokenData()
            {
                Id = existing.Id,
                Logit = logit,
                P = logit
            };

            tokens.Sorted = false;
        }

        public static void SetPenalty(this TokenDataArray tokens, int tokenId, float probability)
        {
            Span<TokenData> span = tokens.Data.Span;
            int index = tokens.GetTokenIndex(tokenId);

            TokenData existing = span[index];

            float newValue = existing.Logit / probability;

            if (existing.Logit <= 0)
            {
                newValue = existing.Logit * probability;
            }

            span[index] = new TokenData()
            {
                Id = existing.Id,
                Logit = newValue,
                P = newValue
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
                Id = existing.Id,
                Logit = probability,
                P = probability
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

        public static TokenData GetTokenData(this TokenDataArray tokens, int tokenId)
        {
            for (int i = 0; i < tokens.Data.Span.Length; i++)
            {
                if (tokens.Data.Span[i].Id == tokenId)
                {
                    return tokens.Data.Span[i];
                }
            }

            throw new KeyNotFoundException();
        }

        private static int GetTokenIndex(this TokenDataArray tokens, int tokenId)
        {
            for (int i = 0; i < tokens.Data.Span.Length; i++)
            {
                if (tokens.Data.Span[i].Id == tokenId)
                {
                    return i;
                }
            }

            throw new KeyNotFoundException();
        }
    }
}