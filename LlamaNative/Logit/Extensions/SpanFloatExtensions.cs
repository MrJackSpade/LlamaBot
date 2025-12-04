using LlamaNative.Logit.Models;

namespace LlamaNative.Logit.Extensions
{
    public static class SpanFloatExtensions
    {
        public static void Add(this Span<float> target, IEnumerable<LogitBias> list)
        {
            foreach (LogitBias bias in list)
            {
                target[bias.LogitId] += bias.Value;
            }
        }

        public static void Update(this Span<float> target, IEnumerable<KeyValuePair<int, string>> list)
        {
            foreach ((int key, string value) in list)
            {
                float v;

                if (string.Equals("-inf", value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.NegativeInfinity;
                }
                else if (string.Equals("+inf", value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.PositiveInfinity;
                }
                else
                {
                    v = float.Parse(value);
                }

                target[key] = v;
            }
        }
    }
}