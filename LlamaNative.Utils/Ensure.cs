using System.Runtime.CompilerServices;

namespace LlamaBot.Shared.Utils
{
    public static class Ensure
    {
        public static Guid NotDefault(Guid o, [CallerArgumentExpression(nameof(o))] string memberName = "")
        {
            if (o == Guid.Empty)
            {
                throw new ArgumentNullException(memberName);
            }

            return o;
        }

        public static T NotNull<T>(T? o, [CallerArgumentExpression(nameof(o))] string memberName = "") where T : class
        {
            if (o == null)
            {
                throw new ArgumentNullException(memberName);
            }

            return o;
        }

        public static T NotNull<T>(T? o, [CallerArgumentExpression(nameof(o))] string memberName = "") where T : struct
        {
            if (!o.HasValue)
            {
                throw new ArgumentNullException(memberName);
            }

            return o.Value;
        }

        public static ulong NotNullOrDefault(ulong? o, [CallerArgumentExpression(nameof(o))] string memberName = "")
        {
            if (!o.HasValue || o.Value == default)
            {
                throw new ArgumentNullException(memberName);
            }

            return o.Value;
        }

        public static T[] NotNullOrEmpty<T>(T[]? o, [CallerArgumentExpression(nameof(o))] string memberName = "")
        {
            if (o == null || o.Length == 0)
            {
                throw new ArgumentNullException(memberName);
            }

            return o;
        }

        public static string NotNullOrWhiteSpace(string? o, [CallerArgumentExpression(nameof(o))] string memberName = "")
        {
            if (string.IsNullOrWhiteSpace(o))
            {
                throw new ArgumentNullException(memberName);
            }

            return o;
        }
    }
}