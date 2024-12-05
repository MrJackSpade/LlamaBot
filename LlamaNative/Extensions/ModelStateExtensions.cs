using LlamaNative.Apis;
using LlamaNative.Interfaces;
using LlamaNative.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Extensions
{
    public static class ModelStateExtensions
    {
        public static Token GetToken(this ModelState modelState, TokenMask mask, int id)
        {
            return new(id, NativeApi.TokenToPiece(modelState.ModelHandle, id), mask);
        }

        public static Span<float> GetLogits(this ModelState modelState)
        {
            int n_vocab = modelState.VocabCount();

            Span<float> logits = NativeApi.GetLogits(modelState.ContextHandle, n_vocab);

            return logits;
        }

        public static int VocabCount(this ModelState modelState)
        {
            return NativeApi.NVocab(modelState.ModelHandle);
        }
    }
}