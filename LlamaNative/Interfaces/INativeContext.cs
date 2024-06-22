using LlamaNative.Interop.Structs;
using LlamaNative.Logit.Collections;
using LlamaNative.Tokens.Interfaces;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Interfaces
{
    public interface INativeContext
    {
        public uint AvailableBuffer { get; }

        IReadOnlyTokenCollection Buffer { get; }

        IReadOnlyTokenCollection Evaluated { get; }

        SafeLlamaContextHandle Handle { get; }

        SafeLlamaModelHandle ModelHandle { get; }

        uint Size { get; }

        void Clear();

        void Dispose();

        void Evaluate(int count = -1);

        Token SampleNext(LogitRuleCollection logitBias = null);

        void SetBufferPointer(uint startIndex);

        void Write(Token token);
    }
}