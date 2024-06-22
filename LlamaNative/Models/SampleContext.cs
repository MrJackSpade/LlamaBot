using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Interfaces;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Models
{
    public struct SampleContext
    {
        public TokenDataArray Candidates { get; set; }

        public SafeLlamaContextHandle ContextHandle { get; set; }

        public IReadOnlyTokenCollection ContextTokens { get; set; }

        public SafeLlamaModelHandle ModelHandle { get; set; }

        public TokenDataArray OriginalCandidates { get; set; }
    }
}