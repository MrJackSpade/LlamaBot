using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers.Temperature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Chat.Models
{
    public class SamplerSetConfiguration
    {
        public string? Pop { get; set; }

        public string? Push { get; set; }

        public List<SamplerSetting> SimpleSamplers { get; set; } = [];

        public SamplerSetting? TokenSelector { get; set; }
    }
}