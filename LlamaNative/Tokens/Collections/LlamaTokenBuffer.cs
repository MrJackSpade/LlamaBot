using Llama.Data.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Collections
{
    public class TokenBuffer : TokenCollection
    {
        public TokenBuffer(uint fixedSize)
        {
            FixedSize = fixedSize;
            Resize();
        }

        public TokenBuffer(IEnumerable<Token> tokens, uint fixedSize) : base(tokens)
        {
            FixedSize = fixedSize;
            Resize();
        }

        public uint FixedSize { get; private set; } = 0;

        public override void Append(Token token)
        {
            base.Append(token);

            if (FixedSize != 0 && _tokens.Count > FixedSize)
            {
                _tokens.RemoveAt(0);
            }
        }

        public override void Clear()
        {
            base.Clear();
            Resize();
        }

        public void Resize()
        {
            while (_tokens.Count < FixedSize)
            {
                _tokens.Add(new Token(-1, null));
            }
        }
    }
}