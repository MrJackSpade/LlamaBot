﻿namespace LlamaNative.Decode.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class BatchDecode<T>
    {
        private List<BatchItem<T>> _items = [];

        public int Count => _items.Count;

        public float[] Embeddings { get; set; }

        public IReadOnlyList<BatchItem<T>> Items => _items;

        public byte[] Logits { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="token"></param>
        /// <param name="position"></param>
        /// <param name="sequenceIds">Defaults to { 0 }</param>
        /// <param name="includeLogits">Defaults to false</param>
        public void AddItem(T token, uint position, int[] sequenceIds = null, bool includeLogits = false)
        {
            BatchItem<T> item = new()
            {
                SequenceIds = sequenceIds ?? [0],
                Position = position,
                Token = token,
                IncludeLogits = includeLogits
            };

            this.AddItem(item);
        }

        public void AddItem(BatchItem<T> item)
        {
            _items.Add(item);
            _items = [.. _items.OrderBy(i => i.Position)];
        }

        public void Clear()
        {
            _items.Clear();
        }

        public BatchDecode<T> Clone(Func<BatchItem<T>, bool> predicate)
        {
            BatchDecode<T> result = new()
            {
                Embeddings = Embeddings,
                Logits = Logits
            };

            foreach (BatchItem<T>? item in Items.Where(predicate))
            {
                result.AddItem(item);
            }

            return result;
        }

        public bool TryRemove(uint positionToRemove, out BatchItem<T> found)
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