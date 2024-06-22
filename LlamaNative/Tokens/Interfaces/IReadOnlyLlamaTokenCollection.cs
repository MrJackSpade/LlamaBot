using Llama.Data.Models;
using LlamaNative.Tokens.Collections;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Interfaces
{
    public interface IReadOnlyTokenCollection : IEnumerable<Token>
    {
        uint Count { get; }

        IEnumerable<int> Ids { get; }

        bool IsNullOrWhiteSpace { get; }

        Token this[int index] { get; }

        TokenCollection Trim(int id = -1);
    }
}