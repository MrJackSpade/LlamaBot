namespace LlamaNative.Logit.Models
{
    public abstract class LogitRule
    {
        public string Key => $"{(int)RuleType}:{LogitId}";

        public LogitRuleLifetime LifeTime { get; set; }

        public int LogitId { get; set; }

        public abstract LogitRuleType RuleType { get; }
    }
}