using LlamaNative.Sampling.Interfaces;
using LlamaNative.Sampling.Models;
using LlamaNative.Sampling.Samplers;
using LlamaNative.Sampling.Samplers.FrequencyAndPresence;
using LlamaNative.Sampling.Samplers.Mirostat;
using LlamaNative.Sampling.Samplers.Repetition;
using LlamaNative.Sampling.Samplers.Temperature;
using LlamaNative.Sampling.Settings;
using System.Reflection;
using System.Text.Json;

namespace LlamaNative.Serialization
{
    public static class SamplerDeserializer
    {
        private static readonly Dictionary<string, Type> _simpleSamplers = [];

        private static readonly Dictionary<string, Type> _tokenSelectors = [];

        static SamplerDeserializer()
        {
            RegisterSelector<TargetedTemperatureSampler>();
            RegisterSelector<TargetedEntropySampler>();
            RegisterSelector<TemperatureTokenSampler>();
            RegisterSelector<GreedySampler>();
            RegisterSelector<GaussianTargetedSampler>();
            RegisterSelector<PowerLawTargetedSampler>();

            RegisterSimple<RepetitionSampler>();
            RegisterSimple<TemperatureSampler>();
            RegisterSimple<ComplexPresenceSampler>();
            RegisterSimple<MinPSampler>();
            RegisterSimple<TfsSampler>();
            RegisterSimple<RepetitionBlockingSampler>();
            RegisterSimple<SubsequenceBlockingSampler>();
        }

        public static object Construct(this SamplerSetting samplerSetting, Type t)
        {
            foreach (ConstructorInfo ci in t.GetConstructors())
            {
                ParameterInfo[] parameters = ci.GetParameters();

                if (parameters.Length != 1)
                {
                    continue;
                }

                Type settingsType = parameters[0].ParameterType;

                object settings = JsonSerializer.Deserialize(samplerSetting.Settings, settingsType)
                                  ?? Activator.CreateInstance(settingsType)!;

                object sampler = Activator.CreateInstance(t, [settings])!;

                return sampler;
            }

            foreach (ConstructorInfo ci in t.GetConstructors())
            {
                if(ci.DeclaringType != t)
                {
                    continue;
                }

                if (ci.GetParameters().Length == 0)
                {
                    return Activator.CreateInstance(t)!;
                }
            }

            throw new NotImplementedException($"Valid constructor not found for {t.FullName}");
        }

        public static ITokenSelector InstantiateSelector(this SamplerSetting samplerSetting)
        {
            if (_tokenSelectors.TryGetValue(samplerSetting.Type, out Type? selectorType))
            {
                return (ITokenSelector)samplerSetting.Construct(selectorType);
            }

            throw new NotImplementedException($"ITokenSelector not found in {nameof(SamplerDeserializer)}.{nameof(_tokenSelectors)}");
        }

        public static T InstantiateSettings<T>(this SamplerSetting samplerSetting)
        {
            return JsonSerializer.Deserialize<T>(samplerSetting.Settings)!;
        }

        public static ISimpleSampler InstantiateSimple(this SamplerSetting samplerSetting)
        {
            if (_simpleSamplers.TryGetValue(samplerSetting.Type, out Type? samplerType))
            {
                return (ISimpleSampler)samplerSetting.Construct(samplerType)!;
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