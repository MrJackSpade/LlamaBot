using LlamaNative.Decode.Interfaces;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Decode.Models
{
    public class FoundBlock
    {
        public uint Offset = 0;

        public uint RequestedSize = 0;

        public uint ActualSize => (uint)(TokenReplacements.Count + RequestedSize);

        public Queue<TokenReplacement> TokenReplacements { get; set; } = new();

        public void AddReplacement(int pos, int value, int[] sequenceIds)
        {
            if (pos < 0)
            {
                throw new ArgumentException("Position must be >= 0");
            }

            //TODO: This is weird, fix it.
            TokenReplacements.Enqueue(new TokenReplacement((uint)pos, new SequencedToken(new Token(value, null, TokenMask.Undefined), sequenceIds)));
        }

        public void AddReplacement(int pos, SequencedToken value)
        {
            if (pos < 0)
            {
                throw new ArgumentException("Position must be >= 0");
            }

            TokenReplacements.Enqueue(new TokenReplacement((uint)pos, value));
        }
    }
}