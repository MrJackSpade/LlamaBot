using LlamaNative.Tokens.Models;

namespace LlamaNative.Models
{
    class DraftToken(Token token, float[] logits)
    {
        public Token Token { get; set; } = token;

        public float[] Logits { get; set; } = logits;
    }
}