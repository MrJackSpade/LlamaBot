using Llama.Data.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Tokens.Interfaces
{
    public interface ITokenCollection : IReadOnlyTokenCollection
    {
        void Append(Token token);
    }
}