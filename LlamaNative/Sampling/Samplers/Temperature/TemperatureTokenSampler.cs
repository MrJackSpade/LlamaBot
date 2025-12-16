using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Samplers.Mirostat;
using LlamaNative.Sampling.Settings;
using System.Diagnostics;
using System.Text;

namespace LlamaNative.Sampling.Samplers.Temperature
{
    public class TemperatureTokenSampler : BaseDynamicSampler<TemperatureTokenSamplerSettings>, ITokenSelector<TemperatureTokenSamplerSettings>
    {
        public TemperatureTokenSampler() : base()
        {
        }

        public int SampleNext(SampleContext sampleContext, TemperatureTokenSamplerSettings settings)
        {
            int token = this.SelectToken(sampleContext, settings, settings.Temperature <= 0, out _);

            StringBuilder sb = new();

            WriteToLog(sampleContext, sampleContext.Candidates.Data.Span, false, token, sb);

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] ({token}); {sb}");

            return token;
        }
    }
}