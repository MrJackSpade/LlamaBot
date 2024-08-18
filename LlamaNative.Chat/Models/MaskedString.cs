using LlamaNative.Tokens.Models;

namespace LlamaNative.Chat.Models
{
    public class MaskedString(string value, TokenMask mask)
    {
        public TokenMask Mask { get; private set; } = mask;

        public string Value { get; private set; } = value ?? throw new ArgumentNullException(nameof(value));
    }
}