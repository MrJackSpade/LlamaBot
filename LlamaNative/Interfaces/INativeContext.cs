using LlamaNative.Interop.Structs;
using LlamaNative.Logit.Collections;
using LlamaNative.Models;
using LlamaNative.Tokens.Interfaces;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Interfaces
{
    public interface INativeContext
    {
        public uint AvailableBuffer { get; }

        IReadOnlyTokenCollection Buffer { get; }

        IReadOnlyTokenCollection Evaluated { get; }

        SafeContextHandle Handle { get; }

        SafeModelHandle ModelHandle { get; }

        uint Size { get; }

        void Clear(bool includeCache);

        void Dispose();

        void Evaluate(int count = -1);

        Token SelectToken(LogitRuleCollection? logitBias, out SampleContext context);

        void SetBufferPointer(uint startIndex);

        void Write(Token token);
    }
}