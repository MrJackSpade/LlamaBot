using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Models
{
    public class TokenSelection
    {
        public TokenSelection(Token selectedToken)
        {
            this.SelectedToken = selectedToken;
        }

        public Token SelectedToken { get; set; }

        public Dictionary<int, TokenData> TokenData { get; set; } = [];
    }
}
