namespace LlamaBotAutomated.Extensions
{
    internal static class DateTimeExtensions
    {
        public static string ToDisplayString(this DateTime? dateTime)
        {
            if(dateTime is null)
            {
                return string.Empty;
            }

            return GetFormattedDateWithOrdinalSuffix(dateTime.Value);
        }

        public static string ToDisplayString(this DateTime dateTime)
        {
            return GetFormattedDateWithOrdinalSuffix(dateTime);
        }

        private static string GetFormattedDateWithOrdinalSuffix(DateTime date)
        {
            int day = date.Day;
            string daySuffix = GetOrdinalSuffix(day);

            return $"{date:dddd, MMMM} {day}{daySuffix} {date:yyyy} at {date:h:mmtt}";
        }

        private static string GetOrdinalSuffix(int day)
        {
            if (day is >= 11 and <= 13)
            {
                return "th"; // Handle exceptions for 11th, 12th, and 13th
            }

            return (day % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };
        }
    }
}
