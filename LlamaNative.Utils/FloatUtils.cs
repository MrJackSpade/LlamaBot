using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
