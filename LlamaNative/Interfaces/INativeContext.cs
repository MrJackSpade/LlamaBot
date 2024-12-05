using LlamaNative.Logit.Collections;
using LlamaNative.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Interfaces
{
    public interface INativeContext
    {
        public uint AvailableBuffer { get; }

        ModelState ModelState { get; }

        uint Size { get; }

        void Clear(bool includeCache);

        void Dispose();

        void Evaluate(int count = -1);

        Token SelectToken(LogitRuleCollection? logitBias, out SampleContext context);

        void SetBufferPointer(uint startIndex);

        void Write(Token token);
    }
}