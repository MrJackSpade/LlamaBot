namespace LlamaNative.Extensions
{
    public static class IDictionaryExtensions
    {
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> target, TKey key, TValue value)
        {
            if (target.ContainsKey(key))
            {
                target[key] = value;
            }
            else
            {
                target.Add(key, value);
            }
        }
    }
}