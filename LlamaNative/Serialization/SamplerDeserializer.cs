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

        private static readonly Dictionary<string, Type> _tokenSelectorSettings = [];

        static SamplerDeserializer()
        {
            // Register token selectors with their settings types
            RegisterSelector<TargetedTemperatureSampler, TargetedTemperatureSamplerSettings>();
            RegisterSelector<TargetedEntropySampler, TargetedEntropySamplerSettings>();
            RegisterSelector<TemperatureTokenSampler, TemperatureTokenSamplerSettings>();
            RegisterSelector<GreedySampler, GreedySamplerSettings>();
            RegisterSelector<PowerLawTargetedSampler, PowerLawTargetedSamplerSettings>();

            RegisterSimple<RepetitionSampler>();
            RegisterSimple<TemperatureSampler>();
            RegisterSimple<ComplexPresenceSampler>();
            RegisterSimple<MinPSampler>();
            RegisterSimple<TfsSampler>();
            RegisterSimple<RepetitionBlockingSampler>();
            RegisterSimple<SubsequenceBlockingSampler>();
        }

        /// <summary>
        /// Constructs an object using a constructor with one settings parameter.
        /// Used for ISimpleSampler which still uses constructor-based settings.
        /// </summary>
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
                if (ci.DeclaringType != t)
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

        /// <summary>
        /// Gets the settings type for a registered token selector.
        /// </summary>
        public static Type? GetSelectorSettingsType(string selectorTypeName)
        {
            _tokenSelectorSettings.TryGetValue(selectorTypeName, out Type? settingsType);
            return settingsType;
        }

        /// <summary>
        /// Instantiates a token selector using its parameterless constructor.
        /// Settings should be passed per-request via SampleNext().
        /// </summary>
        public static ITokenSelector InstantiateSelector(this SamplerSetting samplerSetting)
        {
            if (_tokenSelectors.TryGetValue(samplerSetting.Type, out Type? selectorType))
            {
                return (ITokenSelector)Activator.CreateInstance(selectorType)!;
            }

            throw new NotImplementedException($"ITokenSelector not found in {nameof(SamplerDeserializer)}.{nameof(_tokenSelectors)}");
        }

        /// <summary>
        /// Deserializes the settings object for a token selector from a SamplerSetting.
        /// </summary>
        public static object InstantiateSelectorSettings(this SamplerSetting samplerSetting)
        {
            if (_tokenSelectorSettings.TryGetValue(samplerSetting.Type, out Type? settingsType))
            {
                return JsonSerializer.Deserialize(samplerSetting.Settings, settingsType)
                       ?? Activator.CreateInstance(settingsType)!;
            }

            throw new NotImplementedException($"Settings type not found for {samplerSetting.Type}");
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

        private static void RegisterSelector<TSelector, TSettings>() where TSelector : ITokenSelector
        {
            _tokenSelectors.Add(typeof(TSelector).Name, typeof(TSelector));
            _tokenSelectorSettings.Add(typeof(TSelector).Name, typeof(TSettings));
        }

        private static void RegisterSimple<TSampler>()
        {
            _simpleSamplers.Add(typeof(TSampler).Name, typeof(TSampler));
        }
    }
}