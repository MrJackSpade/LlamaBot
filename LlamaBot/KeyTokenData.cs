using LlamaNative.Interop.Structs;

namespace LlamaBot
{
    internal partial class Program
    {
        private class KeyTokenData(TokenData t)
        {
            /// <summary>
            /// token id
            /// </summary>
            public int Id { get; set; } = t.Id;

            /// <summary>
            /// log-odds of the token
            /// </summary>
            public float Logit { get; set; } = t.Logit;

            /// <summary>
            /// probability of the token
            /// </summary>
            public float P { get; set; } = t.P;
        }
    }
}