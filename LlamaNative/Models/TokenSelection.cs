using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Models
{
    public class TokenSelection(Token selectedToken)
    {
        public Token SelectedToken { get; set; } = selectedToken;

        public Dictionary<int, TokenData> TokenData { get; set; } = [];
    }
}