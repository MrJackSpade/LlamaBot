using LlamaNative.Decode.Interfaces;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Decode.Models
{
    public class TokenReplacement(uint pos, SequencedToken value)
    {
        public uint Pos { get; set; } = pos;

        public SequencedToken Value { get; set; } = value;

        public BatchItem ToBatchItem() => new(Value.Data, Pos, Value.SequenceIds);
    }
}