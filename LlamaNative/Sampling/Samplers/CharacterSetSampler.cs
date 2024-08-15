using LlamaNative.Apis;
using LlamaNative.Interop.Apis;
using LlamaNative.Interop.Structs;
using LlamaNative.Models;
using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Settings;
using LlamaNative.Tokens.Extensions;

namespace LlamaNative.Sampling.Samplers
{
    public enum CharacterSet
    {
        Undefined,

        English
    }

    public class CharacterSetSampler : ISimpleSampler
    {
        public CharacterSetSamplerSettings _settings;

        private readonly Dictionary<IntPtr, int[]> _suppressCache = new();

        public CharacterSetSampler(CharacterSetSamplerSettings settings)
        {
            _settings = settings;
        }

        public void SampleNext(SampleContext context)
        {
            if (_settings.WhiteList.Length == 0 && _settings.BlackList.Length == 0)
            {
                return;
            }

            if (_settings.WhiteList.Length == 1 && _settings.BlackList.Length == 0 && _settings.WhiteList.SingleOrDefault() == CharacterSet.English)
            {
                int[] toSuppress = this.GetOrCreateCache(context.ModelHandle);

                if (toSuppress.Length == 0)
                {
                    return;
                }

                foreach (int i in toSuppress)
                {
                    context.Candidates.SetLogit(i, float.NegativeInfinity);
                }

                SamplingApi.SoftMax(context.Candidates);
            }
            else
            {
                throw new NotImplementedException("Only whitelisting english characters is currently supported");
            }
        }

        private CharacterSet GetCharacterSet(string input)
        {
            // Iterate through each character in the string
            foreach (char c in input)
            {
                // Check if the character is outside the basic Latin and Latin-1 Supplement range
                if (c is (< '\u0000' or > '\u007F') and (< '\u00A0' or > '\u00FF'))
                {
                    // If the character is outside these ranges, it's a non-English character
                    return CharacterSet.Undefined;
                }
            }

            // If no non-English characters were found
            return CharacterSet.English;
        }

        private int[] GetOrCreateCache(SafeModelHandle model)
        {
            if (!_suppressCache.TryGetValue(model.Handle, out int[] toSuppress))
            {
                List<int> temp = [];

                int nvocab = NativeApi.NVocab(model);

                for (int i = 0; i < nvocab; i++)
                {
                    bool isValid = SamplingApi.TryTokenToPiece(model, i, out string result);

                    if (isValid)
                    {
                        CharacterSet characterSet = this.GetCharacterSet(result);

                        if (_settings.WhiteList.Length > 0)
                        {
                            if (!_settings.WhiteList.Contains(characterSet))
                            {
                                temp.Add(i);
                            }
                        }
                        else if (_settings.BlackList.Length > 0)
                        {
                            if (_settings.BlackList.Contains(characterSet))
                            {
                                temp.Add(i);
                            }
                        }
                    }
                }

                toSuppress = temp.ToArray();
                _suppressCache[model.Handle] = toSuppress;
            }

            return toSuppress;
        }
    }
}