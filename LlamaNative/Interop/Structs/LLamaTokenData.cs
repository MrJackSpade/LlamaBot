using System.Runtime.InteropServices;

namespace LlamaNative.Interop.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TokenData(int id, float logit, float p)
    {
        /// <summary>
        /// token id
        /// </summary>
        public int Id = id;

        /// <summary>
        /// log-odds of the token
        /// </summary>
        public float Logit = logit;

        /// <summary>
        /// probability of the token
        /// </summary>
        public float P = p;
    }
}