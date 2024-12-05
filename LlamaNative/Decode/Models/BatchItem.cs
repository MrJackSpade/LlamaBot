using LlamaNative.Tokens.Models;

namespace LlamaNative.Decode.Models
{
    public class BatchItem(Token token, uint pos, int[] seqIds)
    {
        public bool IncludeLogits { get; set; }

        public uint Position { get; set; } = pos;

        public int[] SequenceIds { get; set; } = seqIds;

        public Token Token { get; set; } = token;

        public override string ToString()
        {
            return $"[{Position}] {Token}";
        }
    }
}