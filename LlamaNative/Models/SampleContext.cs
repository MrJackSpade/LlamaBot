using LlamaNative.Decode.Interfaces;
using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Models
{
    public struct SampleContext
    {
        public TokenDataArray Candidates { get; set; }

        public SafeContextHandle ContextHandle { get; set; }

        public KvCacheState KvCache { get; set; }

        public SafeModelHandle ModelHandle { get; set; }

        public TokenDataArray OriginalCandidates { get; set; }
    }
}