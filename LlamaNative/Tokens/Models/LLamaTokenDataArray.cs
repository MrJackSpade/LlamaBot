using LlamaNative.Interop.Structs;
using System.Collections;

namespace LlamaNative.Tokens.Models
{
    public class TokenDataArray : IEnumerable<TokenData>
    {
        public TokenDataArray(TokenData[] data, ulong size, bool sorted)
        {
            Data = data;
            Size = size;
            Sorted = sorted;
        }

        public TokenDataArray(TokenData[] data, bool sorted = false)
        {
            Data = data;
            Size = (ulong)data.Length;
            Sorted = sorted;
        }

        public TokenDataArray(Span<float> logits)
        {
            List<TokenData> candidates = new(logits.Length);

            for (int token_id = 0; token_id < logits.Length; token_id++)
            {
                candidates.Add(new TokenData(token_id, logits[token_id], 0.0f));
            }

            Data = candidates.ToArray();
            Size = (ulong)Data.Length;
            Sorted = false;
        }

        public Memory<TokenData> Data { get; set; }

        public ulong Size { get; set; }

        public bool Sorted { get; set; }

        public TokenData this[ulong index] => Data.Span[(int)index];

        public IEnumerator<TokenData> GetEnumerator()
        {
            TokenData[] data = Data.ToArray();

            return data.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}