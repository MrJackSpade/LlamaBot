namespace LlamaNative.Utils.Extensions
{
    public static class SpanExtensions
    {
        public static List<T> ToList<T>(this Span<T> source)
        {
            return [.. source];
        }

        public static List<T> Where<T>(this Span<T> span, Func<T, bool> predicate)
        {
            List<T> list = [];

            for (int i = 0; i < span.Length; i++)
            {
                T item = span[i];

                if (predicate(item))
                {
                    list.Add(item);
                }
            }

            return list;
        }
    }
}