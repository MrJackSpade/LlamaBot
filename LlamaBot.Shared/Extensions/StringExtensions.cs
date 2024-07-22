namespace LlamaBot.Shared.Extensions
{
    public static class StringExtensions
    {
        public static byte[]? FromBase64(this string? base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
            {
                return null;
            }

            try
            {
                return Convert.FromBase64String(base64String);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}