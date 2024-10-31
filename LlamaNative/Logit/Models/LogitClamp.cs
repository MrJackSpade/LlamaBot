namespace LlamaNative.Logit.Models
{
    public class LogitClamp : LogitRule
    {
        private float _startValue;

        public LogitClamp()
        { }

        public override LogitRuleType RuleType => LogitRuleType.Clamp;

        public LogitClampType Type { get; set; }

        public float GetValue(float newValue)
        {
            return Type switch
            {
                LogitClampType.PreventChange => _startValue,
                LogitClampType.PreventDecrease => Math.Max(_startValue, newValue),
                LogitClampType.PreventIncrease => Math.Min(_startValue, newValue),
                _ => throw new NotImplementedException(),
            };
        }

        public void SetStart(float value)
        {
            _startValue = value;
        }

        public override string ToString()
        {
            return $"[Clamp] {RuleType}";
        }
    }
}