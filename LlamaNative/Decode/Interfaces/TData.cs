using LlamaNative.Tokens.Models;

namespace LlamaNative.Decode.Interfaces
{
    public class SequencedToken(Token data, int[] sequenceIds)
    {
        public Token Data { get; set; } = data;

        public int[] SequenceIds { get; set; } = sequenceIds;

        public override string ToString()
        {
            return $"{Data} [{string.Join(", ", SequenceIds)}]";
        }
    }
}