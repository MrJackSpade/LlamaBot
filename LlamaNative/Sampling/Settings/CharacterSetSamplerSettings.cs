using LlamaNative.Sampling.Samplers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlamaNative.Sampling.Settings
{
    public class CharacterSetSamplerSettings
    {
        public CharacterSet[] BlackList { get; set; } = [];

        public CharacterSet[] WhiteList { get; set; } = [];
    }
}
