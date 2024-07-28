using LlamaNative.Logit.Collections;
using LlamaNative.Logit.Models;
using LlamaNative.Tokens.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Chat.Extensions
{
    internal static class LogitRuleCollectionExtensions
    {
        public static void BlockToken(this LogitRuleCollection collection, Token token, LogitRuleLifetime logitRuleLifetime = LogitRuleLifetime.Token)
        {
            collection.Add(new LogitBias(token.Id, float.PositiveInfinity, logitRuleLifetime, LogitBiasType.Additive));
        }
    }
}
