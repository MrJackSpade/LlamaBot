using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Collections
{
    public class TokenQueue : TokenCollection
    {
        public Token Dequeue()
        {
            Token toReturn = _tokens[0];
            _tokens.RemoveAt(0);
            return toReturn;
        }
    }
}