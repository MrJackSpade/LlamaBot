using LlamaNative.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Sampling.Extensions
{
    public static class SampleContextExtensions
    {
        public static TokenData GetData(this SampleContext sampleContext, int tokenId)
        {
            TokenData[] candidates = sampleContext.Candidates.Data.Span.ToArray();

            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].Id == tokenId)
                {
                    return candidates[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(tokenId));
        }

        public static string GetDisplayString(this SampleContext ctx, int tokenId)
        {
            TokenData tokenData = new();

            for (ulong i = 0; i < ctx.OriginalCandidates.Size; i++)
            {
                if (ctx.OriginalCandidates[i].Id == tokenId)
                {
                    tokenData = ctx.OriginalCandidates[i];
                    break;
                }
            }

            TokenData newTokenData = new();

            for (int i = 0; i < ctx.Candidates.Data.Length; i++)
            {
                if (ctx.Candidates.Data.Span[i].Id == tokenId)
                {
                    newTokenData = ctx.Candidates.Data.Span[i];
                    break;
                }
            }

            Token token = ctx.GetToken(TokenMask.Undefined, tokenData.Id);

            return $"{token.GetEscapedValue()} ({tokenData.P:0.00} => {newTokenData.P:0.00})";
        }

        public static TokenData GetOriginalData(this SampleContext sampleContext, int tokenId)
        {
            TokenDataArray candidates = sampleContext.OriginalCandidates;

            for (ulong i = 0; i < candidates.Size; i++)
            {
                if (candidates[i].Id == tokenId)
                {
                    return candidates[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(tokenId));
        }

        public static float GetOriginalProbability(this SampleContext context, int tokenId)
        {
            foreach (TokenData ltd in context.OriginalCandidates)
            {
                if (ltd.Id == tokenId)
                {
                    return ltd.P;
                }
            }

            throw new InvalidDataException();
        }

        public static Token GetToken(this SampleContext ctx, TokenMask tokenMask, int id)
        {
            return new(id, ctx.ModelHandle.TokenToPiece(id), tokenMask);
        }
    }
}