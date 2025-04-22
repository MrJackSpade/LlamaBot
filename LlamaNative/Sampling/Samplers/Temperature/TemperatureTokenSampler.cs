using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Samplers.Mirostat;
using LlamaNative.Sampling.Settings;
using System.Diagnostics;
using System.Text;

namespace LlamaNative.Sampling.Samplers.Temperature
{
    public class TemperatureTokenSampler(TemperatureTokenSamplerSettings temperatureSamplerSettings) : BaseDynamicSampler<TemperatureTokenSamplerSettings>(0, temperatureSamplerSettings), ITokenSelector
    {
        public int SampleNext(SampleContext sampleContext)
        {
            int token = this.SelectToken(sampleContext, _settings.Temperature <= 0, out _);

            StringBuilder sb = new();

            WriteToLog(sampleContext, sampleContext.Candidates.Data.Span, false, token, sb);

            Debug.WriteLine($"[{sampleContext.ContextTokens.Trim().Count:00000}] ({token}); {sb}");

            return token;
        }
    }
}