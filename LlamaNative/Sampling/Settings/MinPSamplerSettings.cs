﻿namespace LlamaNative.Sampling.Settings
{
    public class MinPSamplerSettings
    {
        public float MinP { get; set; } = 0.03f;

        public Dictionary<int, float> MinPs { get; set; } = [];
    }
}