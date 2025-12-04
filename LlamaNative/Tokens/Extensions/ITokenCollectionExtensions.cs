namespace LlamaNative.Tokens.Extensions
{
    public static class ITokenCollectionExtensions
    {
        public static int FindIndex<T>(this IEnumerable<T> source, uint start, Func<T, bool> func)
        {
            uint i = 0;
            foreach (T t in source)
            {
                if (i >= start && func.Invoke(t))
                {
                    return (int)i;
                }

                i++;
            }

            return -1;
        }
    }
}