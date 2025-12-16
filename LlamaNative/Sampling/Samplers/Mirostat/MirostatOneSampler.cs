using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using System.Diagnostics;

namespace LlamaNative.Sampling.Samplers.Mirostat
{
    /// <summary>
    /// Mirostat v1 sampler implementation.
    /// State is stored in the settings object for per-channel isolation.
    /// </summary>
    public class MirostatOneSampler : ITokenSelector<MirostatSamplerSettings>
    {
        public static int Clamp(float k)
        {
            if (k <= 0)
            {
                return 0;
            }
            else if (k >= int.MaxValue)
            {
                return int.MaxValue;
            }
            else
            {
                return (int)k;
            }
        }

        public int SampleNext(SampleContext sampleContext, MirostatSamplerSettings settings)
        {
            // Initialize mu on first call with these settings
            if (!settings.MuInitialized)
            {
                settings.Mu = settings.InitialMu;
                settings.MuInitialized = true;
            }

            Span<TokenData> candidateSpan = sampleContext.Candidates.Data.Span;

            SamplingApi.Temperature(sampleContext.Candidates, settings.Temperature);

            float tau = settings.Tau;
            float eta = settings.Eta;
            float m = settings.M;

            float n = sampleContext.Candidates.Data.Length;

            SamplingApi.SoftMax(sampleContext.Candidates, true);

            float sum_ti_bi = 0.0f;
            float sum_ti_sq = 0.0f;

            for (int i = 0; i < m - 1 && i < (int)sampleContext.Candidates.Size - 1; i++)
            {
                float ti = (float)Math.Log((i + 2) / (double)(i + 1));
                float b_i = (float)Math.Log(candidateSpan[i].P / candidateSpan[i + 1].P);
                sum_ti_bi += ti * b_i;
                sum_ti_sq += ti * ti;
            }

            // Estimate s_hat using the most probable m tokens
            float hat = sum_ti_bi / sum_ti_sq;

            // Compute k from the estimated s_hat and target surprise value
            float epsilon_hat = hat - 1;

            float k = (float)Math.Pow(epsilon_hat * Math.Pow(2, settings.Mu) / (1 - Math.Pow(n, -epsilon_hat)), 1 / hat);

            Debug.WriteLine($"k: {k}");

            bool topOnly = false;
            int top_x = 0;

            if (settings.PreserveWords)
            {
                top_x = SamplingApi.TokenGreedy(sampleContext.Candidates);
                topOnly = !this.CheckIfWord(sampleContext.ModelHandle, top_x, settings);
            }

            int x;

            if (topOnly)
            {
                x = top_x;
            }
            else
            {
                int ki = Clamp(k);
                // Sample the next word X using top-k sampling
                SamplingApi.TopK(sampleContext.Candidates, ki, 1);
                x = SamplingApi.Token(sampleContext.Candidates);
            }

            // Compute error as the difference between observed surprise and target surprise value
            int x_idx = 0;

            for (int i = 0; i < (int)sampleContext.Candidates.Size; i++)
            {
                if (sampleContext.Candidates.Data.Span[i].Id == x)
                {
                    x_idx = i;
                    break;
                }
            }

            float observed_surprise = -(float)(Math.Log(sampleContext.Candidates.Data.Span[x_idx].P) / Math.Log(2));
            float e = observed_surprise - tau;

            // Update mu using the learning rate and error (in settings for persistence)
            settings.Mu -= eta * e;

            Debug.WriteLine($"mu: {settings.Mu}");

            return x;
        }

        private bool CheckIfWord(SafeModelHandle ctx, int id, MirostatSamplerSettings settings)
        {
            if (!settings.IsWordsCache.TryGetValue(id, out bool word))
            {
                string value = ctx.TokenToPiece(id);
                word = !string.IsNullOrWhiteSpace(value) && value[0] == ' ';
                settings.IsWordsCache.Add(id, word);
            }

            return word;
        }
    }
}