using LlamaNative.Logit.Models;
using LlamaNative.Tokens.Models;
using System.Diagnostics;

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

        public static Dictionary<Token, float> Extract(this Span<float> source, IEnumerable<Token> list)
        {
            Dictionary<Token, float> toReturn = new();

            foreach (Token Token in list)
            {
                toReturn.Add(Token, source[Token.Id]);
            }

            return toReturn;
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

        public static void Update(this Span<float> target, IEnumerable<KeyValuePair<Token, float>> list)
        {
            foreach (KeyValuePair<Token, float> Token in list)
            {
                if (target[Token.Key.Id] != Token.Value)
                {
                    Debug.Write($"Adjusting logit [{Token.Key.Id}]; '{target[Token.Key.Id]}' => '{Token.Value}'");
                    target[Token.Key.Id] = Token.Value;
                }
            }
        }
    }
}