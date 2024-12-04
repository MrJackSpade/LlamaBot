using LlamaNative.Decode.Interfaces;
using LlamaNative.Tokens.Models;
using System.Collections;

namespace LlamaNative.Decode.Collections
{
    public class PointerArray : IEnumerable<SequencedToken>
    {
        private readonly SequencedToken[] _backingData;

        public PointerArray(uint length, params SequencedToken[] array)
        {
            _backingData = new SequencedToken[length];

            for (int i = 0; i < array.Length; i++)
            {
                _backingData[i] = array[i];
            }
        }

        public int Count => (int)Pointer;

        public uint Length => (uint)_backingData.Length;

        public uint Pointer { get; set; }

        public SequencedToken this[uint index]
        {
            get => _backingData[index];
            set => _backingData[index] = value;
        }

        public void Clear()
        {
            Pointer = 0;
        }

        public void Fill(Token item)
        {
            for (int i = 0; i < _backingData.Length; i++)
            {
                _backingData[i] = new(item, [0]);
            }
        }

        public IEnumerator<SequencedToken> GetEnumerator()
        {
            return ((IEnumerable<SequencedToken>)_backingData).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _backingData.GetEnumerator();
        }

        public Span<SequencedToken> Slice(int startIndex, int length)
        {
            return _backingData.AsSpan().Slice(startIndex, length);
        }

        public void Slide(uint v)
        {
            for (uint i = v; i < _backingData.Length - v; i++)
            {
                _backingData[i - v] = _backingData[i];
            }

            Pointer -= v;
        }

        public void Slide(uint start, uint count)
        {
            for (uint i = start; i < _backingData.Length - count; i++)
            {
                _backingData[i - count] = _backingData[i];
            }

            Pointer -= count;
        }

        public void Write(SequencedToken element)
        {
            this[Pointer] = element;
            Pointer++;
        }
    }
}