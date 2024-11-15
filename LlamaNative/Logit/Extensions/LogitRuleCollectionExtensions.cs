﻿using LlamaNative.Logit.Collections;
using LlamaNative.Logit.Models;
using LlamaNative.Tokens.Extensions;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Logit.Extensions
{
    public static class LogitRuleCollectionExtensions
    {
        public static void ApplyBias(this LogitRuleCollection logitRules, TokenDataArray candidates)
        {
            //Apply bias
            foreach (LogitBias bias in logitRules.OfType<LogitBias>())
            {
                candidates.SetBias(bias.LogitId, bias.Value, bias.LogitBiasType);
            }
        }

        public static void ApplyClamp(this LogitRuleCollection logitRules, TokenDataArray candidates)
        {
            //Apply clamping
            foreach (LogitClamp clamp in logitRules.OfType<LogitClamp>())
            {
                float nv = candidates.GetProbability(clamp.LogitId);
                float cv = clamp.GetValue(nv);

                if (cv != nv)
                {
                    candidates.SetProbability(clamp.LogitId, cv);
                }
            }
        }

        public static void ApplyPenalty(this LogitRuleCollection logitRules, TokenDataArray candidates)
        {
            //Apply penalty
            foreach (LogitPenalty penalty in logitRules.OfType<LogitPenalty>())
            {
                candidates.SetPenalty(penalty.LogitId, penalty.Value);
            }
        }

        public static void StartClamp(this LogitRuleCollection logitRules, TokenDataArray candidates)
        {
            foreach (LogitClamp clamp in logitRules.OfType<LogitClamp>())
            {
                clamp.SetStart(candidates.GetProbability(clamp.LogitId));
            }
        }
    }
}