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

        bool EndsWith(int[] ids);

        TokenCollection Trim(int id = -1);
    }
}