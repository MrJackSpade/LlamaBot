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
            Ordered = sorted;
        }

        public TokenDataArray(TokenData[] data, bool sorted = false)
        {
            Data = data;
            Size = (ulong)data.Length;
            Ordered = sorted;
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
        }

        public Memory<TokenData> Data { get; set; }

        /// <summary>
        /// True if the tokens have been ordered by descending logit values
        /// False if the tokens need to be ordered
        /// </summary>
        public bool Ordered { get; set; }

        public ulong Size { get; set; }

        public TokenData this[ulong index] => Data.Span[(int)index];

        public IEnumerator<TokenData> GetEnumerator()
        {
            TokenData[] data = Data.ToArray();

            return data.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}