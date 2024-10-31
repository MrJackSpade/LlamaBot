using System.Diagnostics;

namespace LlamaNative.Tokens.Models
{
    [DebuggerDisplay("{Value}")]
    public class Token(int id, string? value, TokenMask mask)
    {
        public static readonly Token Null = new(-1, null, TokenMask.Undefined);

        public int Id { get; private set; } = id;

        public TokenMask Mask { get; set; } = mask;

        public string? Value { get; private set; } = value;

        public static bool operator !=(Token x, Token y)
        {
            return !(x == y);
        }

        public static bool operator ==(Token x, Token y)
        {
            return x?.Id == y?.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is Token o && this == o;
        }

        public string? GetEscapedValue()
        {
            //TODO: Leverage native escaping
            string? toReturn = Value;

            toReturn = toReturn?.Replace("\r", "\\r");
            toReturn = toReturn?.Replace("\n", "\\n");

            return toReturn;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string? ToString()
        {
            return Value;
        }
    }
}