using LlamaNative.Sampling.Samplers;

namespace LlamaNative.Sampling.Settings
{
    public class CharacterSetSamplerSettings
    {
        public CharacterSet[] BlackList { get; set; } = [];

        public CharacterSet[] WhiteList { get; set; } = [];
    }
}
