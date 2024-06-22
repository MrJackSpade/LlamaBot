namespace LlamaNative.Decode.Models
{
    public class TokenReplacement(uint pos, int value)
    {
        public uint Pos { get; set; } = pos;

        public int Value { get; set; } = value;
    }
}