﻿using LlamaNative.Sampling.Models;

namespace LlamaNative.Chat.Models
{
    public class SamplerSetConfiguration
    {
        public string? Pop { get; set; }

        public string? Push { get; set; }

        public List<SamplerSetting> SimpleSamplers { get; set; } = [];

        public SamplerSetting? TokenSelector { get; set; }

        public Dictionary<int, string> LogitBias { get; set; } = [];

        public Dictionary<char, string> CharBias { get; set; } = [];
    }
}