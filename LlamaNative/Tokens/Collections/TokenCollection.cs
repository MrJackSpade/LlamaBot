using LlamaNative.Tokens.Interfaces;
using LlamaNative.Tokens.Models;
using System.Collections;

namespace LlamaNative.Tokens.Collections
{
    public class TokenCollection : ITokenCollection
    {
        protected List<Token> _tokens = [];

        public TokenCollection(IEnumerable<Token> tokens)
        {
            foreach (Token token in tokens)
            {
                this.Append(token);
            }
        }

        public TokenCollection()
        {
        }

        public uint Count => (uint)_tokens.Count;

        public IEnumerable<int> Ids => _tokens.Select(t => t.Id);

        public bool IsNullOrEmpty => _tokens.Count == 0;

        public bool IsNullOrWhiteSpace => string.IsNullOrWhiteSpace(this.ToString());

        public Token this[int index]
        {
            get => _tokens[index];
            set => _tokens[index] = value;
        }

        public static TokenCollection operator +(TokenCollection a, Token b)
        {
            TokenCollection toReturn = new();

            foreach (Token token in a)
            {
                toReturn.Append(token);
            }

            toReturn.Append(b);

            return toReturn;
        }

        public static TokenCollection operator +(Token a, TokenCollection b)
        {
            TokenCollection toReturn = new();

            toReturn.Append(a);

            foreach (Token token in b)
            {
                toReturn.Append(token);
            }

            return toReturn;
        }

        public virtual void Append(Token token) => _tokens.Add(token);

        public virtual void Clear() => _tokens.Clear();

        public bool EndsWith(int[] ids)
        {
            if (_tokens.Count < ids.Length)
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                int offset = i + 1;
                int token = _tokens[^offset].Id;
                int checkToken = ids[^offset];

                if (token != checkToken)
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerator<Token> GetEnumerator() => ((IEnumerable<Token>)_tokens).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_tokens).GetEnumerator();

        public void Shift(Token token)
        {
            _tokens.RemoveAt(0);
            _tokens.Add(token);
        }

        public string ToEscapedString() => string.Join("", _tokens.Select(t => t.GetEscapedValue()));

        public override string ToString() => string.Join("", _tokens.Select(t => t.Value));

        public virtual TokenCollection Trim(int id = -1)
        {
            TokenCollection Tokens = new();

            List<Token> tokens = [];

            bool isStarted = false;

            foreach (Token token in _tokens)
            {
                if (token.Id != id)
                {
                    isStarted = true;
                }

                if (isStarted)
                {
                    tokens.Add(token);
                }

                if (token.Id != id)
                {
                    foreach (Token lToken in tokens)
                    {
                        Tokens.Append(lToken);
                    }

                    tokens.Clear();
                }
            }

            return Tokens;
        }
    }
}