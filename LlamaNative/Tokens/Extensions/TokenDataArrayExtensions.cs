using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Logit.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Extensions
{
    public static class TokenDataArrayExtensions
    {
        public static TokenData GetMostLikely(this TokenDataArray tokens)
        {
            if (!tokens.Calculated)
            {
                SamplingApi.SoftMax(tokens, false);
            }

            if (tokens.Ordered)
            {
                return tokens.Data.Span[0];
            }
            else
            {
                Span<TokenData> span = tokens.Data.Span;

                TokenData mostLikely = span[0];

                for (int i = 1; i < span.Length; i++)
                {
                    TokenData check = span[i];

                    if (check.P > mostLikely.P)
                    {
                        mostLikely = check;
                    }
                }

                return mostLikely;
            }
        }

        public static float GetProbability(this TokenDataArray tokens, int tokenId)
        {
            Span<TokenData> span = tokens.Data.Span;
            int index = tokens.GetTokenIndex(tokenId);
            TokenData existing = span[index];
            return existing.Logit;
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

        public static void SetBias(this TokenDataArray tokens, int tokenId, float probability, LogitBiasType logitBiasType)
        {
            Span<TokenData> span = tokens.Data.Span;

            int index = tokens.GetTokenIndex(tokenId);

            TokenData existing = span[index];

            int mod = existing.Logit > 0 ? 1 : -1;

            float newLogit = logitBiasType switch
            {
                LogitBiasType.Additive => existing.Logit + probability,
                LogitBiasType.Multiplicative => existing.Logit * probability * mod,
                _ => throw new NotImplementedException()
            };

            if (existing.Logit == newLogit)
            {
                return;
            }

            tokens.SetLogitAtIndex(index, newLogit);
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

            candidates.Ordered = false;
        }

        public static void SetLogitAtIndex(this TokenDataArray tokens, int index, float logit)
        {
            Span<TokenData> span = tokens.Data.Span;
            TokenData existing = span[index];

            span[index] = new TokenData()
            {
                Id = existing.Id,
                Logit = logit
            };

            tokens.Ordered = false;
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

            tokens.Ordered = false;
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

            tokens.Ordered = false;
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