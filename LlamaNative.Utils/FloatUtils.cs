namespace LlamaNative.Utils
{
    public static class FloatUtils
    {
        public static float Parse(string value)
        {
            if (string.Equals(value, "-inf", StringComparison.OrdinalIgnoreCase))
            {
                return float.NegativeInfinity;
            }

            if (string.Equals(value, "inf", StringComparison.OrdinalIgnoreCase))
            {
                return float.PositiveInfinity;
            }

            return float.Parse(value);
        }
    }
}