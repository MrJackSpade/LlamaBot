using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Collections
{
    public class TokenBuffer : TokenCollection
    {
        public TokenBuffer(uint fixedSize)
        {
            FixedSize = fixedSize;
            this.Resize();
        }

        public TokenBuffer(IEnumerable<Token> tokens, uint fixedSize) : base(tokens)
        {
            FixedSize = fixedSize;
            this.Resize();
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
            this.Resize();
        }

        public void Resize()
        {
            while (_tokens.Count < FixedSize)
            {
                _tokens.Add(Token.Null);
            }
        }
    }
}