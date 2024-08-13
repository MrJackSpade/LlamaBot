using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Utils.Extensions
{
    public static class SpanExtensions
    {
        public static List<T> ToList<T>(this Span<T> source)
        {
            return [.. source];
        }
    }
}
