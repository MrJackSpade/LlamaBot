namespace LlamaBot.Extensions
{
    internal static class TimeSpanExtensions
    {
        public static string ToDisplayString(this TimeSpan value)
        {
            if (value.TotalHours < 1)
            {
                return "";
            }

            if (value.TotalDays >= 365)
            {
                int years = (int)(value.TotalDays / 365);
                return $"{years} {(years == 1 ? "year" : "years")} later";
            }
            else if (value.TotalDays >= 30)
            {
                int months = (int)(value.TotalDays / 30);
                return $"{months} {(months == 1 ? "month" : "months")} later";
            }
            else if (value.TotalDays >= 7)
            {
                int weeks = (int)(value.TotalDays / 7);
                return $"{weeks} {(weeks == 1 ? "week" : "weeks")} later";
            }
            else if (value.TotalDays >= 1)
            {
                int days = (int)value.TotalDays;
                return $"{days} {(days == 1 ? "day" : "days")} later";
            }
            else
            {
                int hours = (int)value.TotalHours;
                return $"{hours} {(hours == 1 ? "hour" : "hours")} later";
            }
        }
    }
}
