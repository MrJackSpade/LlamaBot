using LlamaNative.Logit.Collections;
using LlamaNative.Logit.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Chat.Extensions
{
    internal static class LogitRuleCollectionExtensions
    {
        public static void BlockToken(this LogitRuleCollection collection, Token token, LogitRuleLifetime logitRuleLifetime = LogitRuleLifetime.Token)
        {
            collection.Add(new LogitBias(token.Id, float.NegativeInfinity, logitRuleLifetime, LogitBiasType.Additive));
        }
    }
}