using LlamaNative.Decode.Utils;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Decode.Interfaces
{
    public class KvCacheState
    {
        private readonly SequencedToken[] _backingData;

        private readonly SequencedToken _defaultToken;

        public IEnumerable<Token> GetSequence(int seqId)
        {
            //Should this be ordered?
            return _backingData.Where(s => s.SequenceIds.Contains(seqId)).Select(s => s.Data);
        }

        private readonly HashSet<uint> _relocated;

        private readonly KvCacheTransformation<SequencedToken>?[] _transformations;

        public uint Length => (uint)_transformations.Length;

        public KvCacheState(uint size, Token defaultToken)
        {
            _backingData = new SequencedToken[size];
            _defaultToken = new SequencedToken(defaultToken, [0]);

            for (int i = 0; i < size; i++)
            {
                _backingData[i] = _defaultToken;
            }

            _transformations = new KvCacheTransformation<SequencedToken>[size];
            _relocated = new HashSet<uint>(_backingData.Length);
        }

        public SequencedToken this[uint index]
        {
            get => _backingData[index];
            set => _backingData[index] = value;
        }

        public void ClearTransformations()
        {
            for (int i = 0; i < _transformations.Length; i++)
            {
                _transformations[i] = null;
            }

            _relocated.Clear();
        }

        public IEnumerable<KvCacheTransformation<SequencedToken>> GetMoves()
        {
            foreach (KvCacheTransformation<SequencedToken>? transform in _transformations.Where(s => (s?.Delta ?? 0) != 0))
            {
                yield return transform!;
            }
        }

        public bool IsDefault(uint index)
        {
            return Equals(_backingData[index], _defaultToken);
        }

        public bool IsMoved(uint index)
        {
            return _relocated.Contains(index);
        }

        public bool IsSet(uint index)
        {
            return _transformations[index] != null;
        }

        public void Move(uint oldIndex, uint newIndex)
        {
            if (this.IsSet(newIndex))
            {
                throw new Exception("New location already has token set");
            }

            if (this.IsMoved(oldIndex))
            {
                throw new Exception("Old location has already been moved");
            }

            _relocated.Add(oldIndex);
            _transformations[newIndex] = new KvCacheTransformation<SequencedToken>(_backingData[oldIndex], oldIndex, newIndex);
        }

        public void Pin(uint index)
        {
            if (this.IsSet(index))
            {
                throw new Exception("New location already has token set");
            }

            if (this.IsMoved(index))
            {
                throw new Exception("Old location has already been moved");
            }

            _relocated.Add(index);
            _transformations[index] = new KvCacheTransformation<SequencedToken>(_backingData[index], index);
        }
    }
}