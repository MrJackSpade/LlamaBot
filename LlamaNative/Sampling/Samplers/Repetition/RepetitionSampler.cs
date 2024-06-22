﻿using LlamaNative.Interop.Apis;
using LlamaNative.Models;
using LlamaNative.Samplers.Settings;
using LlamaNative.Sampling.Extensions;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Tokens.Collections;

namespace LlamaNative.Sampling.Samplers.Repetition
{
    public class RepetitionSampler : ISimpleSampler
    {
        private readonly HashSet<int> _exclude = new();

        private readonly HashSet<int> _include = new();

        private readonly RepetitionSamplerSettings _settings;

        public RepetitionSampler(RepetitionSamplerSettings settings)
        {
            _settings = settings;

            foreach (int i in settings.Exclude)
            {
                _exclude.Add(i);
            }

            foreach (int i in settings.Include)
            {
                _include.Add(i);
            }
        }

        public void SampleNext(SampleContext sampleContext)
        {
            TokenCollection sampleTokens = sampleContext.ContextTokens.Trim();

            LastTokens lastTokens = this.GetLastTokens(sampleTokens, _settings.RepeatPenaltyWindow, _include, _exclude);

            SamplingApi.RepetitionPenalties(sampleContext.Candidates, lastTokens.Ids, _settings.RepeatPenalty, _settings.FrequencyPenalty, _settings.PresencePenalty);
        }
    }
}