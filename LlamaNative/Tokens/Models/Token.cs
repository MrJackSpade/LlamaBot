using System.Diagnostics;

namespace LlamaNative.Tokens.Models
{
    [DebuggerDisplay("{Value}")]
    public class Token(int id, string? value)
    {
        public int Id { get; private set; } = id;

        public string? Value { get; private set; } = value;

        public static bool operator !=(Token x, Token y) => !(x == y);

        public static bool operator ==(Token x, Token y) => x?.Id == y?.Id;

        public override bool Equals(object? obj) => obj is Token o && this == o;

        public string? GetEscapedValue()
        {
            //TODO: Leverage native escaping
            string? toReturn = Value;

            toReturn = toReturn?.Replace("\r", "\\r");
            toReturn = toReturn?.Replace("\n", "\\n");

            return toReturn;
        }

        public override int GetHashCode() => Id;

        public override string? ToString() => Value;
    }
}