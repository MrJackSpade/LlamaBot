namespace LlamaNative.Logit.Models
{
    public class LogitBias : LogitRule
    {
        public LogitBias(int id, float value, LogitRuleLifetime lifeTime, LogitBiasType logitBiasType)
        {
            LifeTime = lifeTime;
            LogitId = id;
            Value = value;
            LogitBiasType = logitBiasType;
        }

        public LogitBias()
        { }

        public LogitBiasType LogitBiasType { get; set; }

        public override LogitRuleType RuleType => LogitRuleType.Bias;

        public float Value { get; set; }

        public override string ToString() => $"[Bias] {Value}";
    }
}