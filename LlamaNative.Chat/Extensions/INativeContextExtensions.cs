using LlamaNative.Chat.Models;
using LlamaNative.Extensions;
using LlamaNative.Interfaces;

namespace LlamaNative.Chat.Extensions
{
    public static class INativeContextExtensions
    {
        public static void Write(this INativeContext context, MaskedString maskedString)
        {
            context.Write(maskedString.Mask, maskedString.Value);
        }
    }
}