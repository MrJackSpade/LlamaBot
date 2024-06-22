using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TokenData
    {
        /// <summary>
        /// token id
        /// </summary>
        public int id;

        /// <summary>
        /// log-odds of the token
        /// </summary>
        public float logit;

        /// <summary>
        /// probability of the token
        /// </summary>
        public float p;

        public TokenData(int id, float logit, float p)
        {
            this.id = id;
            this.logit = logit;
            this.p = p;
        }
    }
}