using LlamaNative.Tokens.Models;
using LlamaNative.Utils;

namespace LlamaNative.Decode.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class BatchDecode
    {
        private List<BatchItem> _items = [];

        public int Count => _items.Count;

        public float[] Embeddings { get; set; }

        public IReadOnlyList<BatchItem> Items => _items;

        public byte[] Logits { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="token"></param>
        /// <param name="position"></param>
        /// <param name="sequenceIds">Defaults to { 0 }</param>
        /// <param name="includeLogits">Defaults to false</param>
        public void AddItem(Token token, uint position, int[] sequenceIds, bool includeLogits = false)
        {
            Ensure.NotNullOrEmpty(sequenceIds);

            BatchItem item = new(token, position, sequenceIds)
            {
                IncludeLogits = includeLogits
            };

            this.AddItem(item);
        }

        public void AddItem(BatchItem item)
        {
            _items.Add(item);
            _items = [.. _items.OrderBy(i => i.Position)];
        }

        public void Clear()
        {
            _items.Clear();
        }

        public BatchDecode Clone(Func<BatchItem, bool> predicate)
        {
            BatchDecode result = new()
            {
                Embeddings = Embeddings,
                Logits = Logits
            };

            foreach (BatchItem? item in Items.Where(predicate))
            {
                result.AddItem(item);
            }

            return result;
        }

        public bool TryRemove(uint positionToRemove, out BatchItem found)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Position == positionToRemove)
                {
                    found = _items[i];

                    _items.RemoveAt(i);

                    return true;
                }
            }

            found = null;

            return false;
        }
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}