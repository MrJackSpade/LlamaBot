namespace LlamaNative.Logit.Models
{
    public class LogitPenalty : LogitRule
    {
        public LogitPenalty(int id, float value, LogitRuleLifetime lifeTime)
        {
            LifeTime = lifeTime;
            LogitId = id;
            Value = value;
        }

        public LogitPenalty()
        { }

        public override LogitRuleType RuleType => LogitRuleType.Penalty;

        public float Value { get; set; }

        public override string ToString() => $"[Penalty] {Value}";
    }
}