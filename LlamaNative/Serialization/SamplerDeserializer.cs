using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Samplers.FrequencyAndPresence;
using LlamaNative.Sampling.Samplers.Mirostat;
using LlamaNative.Sampling.Samplers.Repetition;
using LlamaNative.Sampling.Samplers.Temperature;
using LlamaNative.Sampling.Samplers;
using System.Reflection;
using System.Text.Json;
using LlamaNative.Sampling.Models;

namespace LlamaNative.Serialization
{
    public static class SamplerDeserializer
    {
        private static readonly Dictionary<string, Type> _simpleSamplers = [];

        private static readonly Dictionary<string, Type> _tokenSelectors = [];

        static SamplerDeserializer()
        {
            RegisterSelector<TargetedTempSampler>();
            RegisterSelector<TemperatureSampler>();

            RegisterSimple<RepetitionSampler>();
            RegisterSimple<ComplexPresenceSampler>();
            RegisterSimple<MinPSampler>();
            RegisterSimple<TfsSampler>();
            RegisterSimple<RepetitionBlockingSampler>();
        }

        public static T Construct<T>(this SamplerSetting samplerSetting)
        {
            foreach (ConstructorInfo ci in typeof(T).GetConstructors())
            {
                ParameterInfo[] parameters = ci.GetParameters();

                if (parameters.Length != 1)
                {
                    continue;
                }

                Type settingsType = parameters[0].ParameterType;

                object settings = JsonSerializer.Deserialize(samplerSetting.Settings, settingsType)!;

                T sampler = (T)Activator.CreateInstance(typeof(T), [settings])!;

                return sampler;
            }

            throw new NotImplementedException();
        }

        public static ITokenSelector InstantiateSelector(this SamplerSetting samplerSetting)
        {
            if (_tokenSelectors.TryGetValue(samplerSetting.Type, out Type? selectorType))
            {
                return (ITokenSelector)Activator.CreateInstance(selectorType)!;
            }

            throw new NotImplementedException();
        }

        public static T InstantiateSettings<T>(this SamplerSetting samplerSetting)
        {
            return JsonSerializer.Deserialize<T>(samplerSetting.Settings)!;
        }

        public static ISimpleSampler InstantiateSimple(this SamplerSetting samplerSetting)
        {
            if (_simpleSamplers.TryGetValue(samplerSetting.Type, out Type? samplerType))
            {
                return (ISimpleSampler)Activator.CreateInstance(samplerType)!;
            }

            throw new NotImplementedException();
        }

        private static void RegisterSelector<TSelector>()
        {
            _tokenSelectors.Add(typeof(TSelector).Name, typeof(TSelector));
        }

        private static void RegisterSimple<TSampler>()
        {
            _simpleSamplers.Add(typeof(TSampler).Name, typeof(TSampler));
        }
    }
}