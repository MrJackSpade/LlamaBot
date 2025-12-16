using LlamaBot.Extensions;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace LlamaBot.Plugins.Commands.Setting
{
    internal class SettingCommandProvider : ICommandProvider<SettingCommand>
    {
        private object? _defaultSamplerSettings;

        private ILlamaBotClient? _llamaBotClient;

        private Type? _samplerSettingsType;

        public string Command => "setting";

        public string Description => "View or modify sampler settings for this channel";

        public SlashCommandOption[] SlashCommandOptions => [];

        public Task<CommandResult> OnCommand(SettingCommand command)
        {
            if (_llamaBotClient == null || _defaultSamplerSettings == null || _samplerSettingsType == null)
            {
                return Task.FromResult(CommandResult.Error("Command provider not initialized"));
            }

            ulong channelId = command.Channel.GetChannelId();
            ChannelSettingsCollection csc = _llamaBotClient.ChannelSettings;

            // Get current settings for this channel
            object currentSettings = csc.GetSamplerSettings(channelId, _defaultSamplerSettings, _samplerSettingsType);

            // If no name specified, show current settings
            if (string.IsNullOrWhiteSpace(command.Name))
            {
                return Task.FromResult(CommandResult.Success(FormatSettings(currentSettings)));
            }

            // Find the property to modify (case-insensitive, must be settable)
            PropertyInfo? property = _samplerSettingsType.GetProperties()
                .FirstOrDefault(p => p.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && IsSettableProperty(p));

            if (property == null)
            {
                StringBuilder validSettings = new();
                validSettings.AppendLine("Unknown setting. Valid settings:");
                foreach (PropertyInfo prop in GetSettableProperties(_samplerSettingsType))
                {
                    validSettings.AppendLine($"  • {prop.Name} ({prop.PropertyType.Name})");
                }
                return Task.FromResult(CommandResult.Error(validSettings.ToString()));
            }

            // If no value specified, show current value
            if (string.IsNullOrWhiteSpace(command.Value))
            {
                object? currentValue = property.GetValue(currentSettings);
                return Task.FromResult(CommandResult.Success($"**{property.Name}**: {currentValue}"));
            }

            // Parse and set the new value
            try
            {
                object? newValue = ConvertValue(command.Value, property.PropertyType);
                property.SetValue(currentSettings, newValue);

                // Persist the updated settings
                csc.SetSamplerSettings(channelId, currentSettings);
                csc.SaveSettings(channelId);

                return Task.FromResult(CommandResult.Success($"Set **{property.Name}** = {newValue}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CommandResult.Error($"Failed to set '{property.Name}': {ex.Message}"));
            }
        }

        public Task<InitializationResult> OnInitialize(InitializationEventArgs args)
        {
            _llamaBotClient = args.LlamaBotClient;

            // Extract default settings type from configuration
            // We need to access the chat settings to get the sampler configuration
            try
            {
                // Access the default sampler settings via reflection since it's private
                Type clientType = _llamaBotClient.GetType();
                FieldInfo? settingsField = clientType.GetField("_defaultSamplerSettings", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo? typeField = clientType.GetField("_samplerSettingsType", BindingFlags.NonPublic | BindingFlags.Instance);

                if (settingsField != null && typeField != null)
                {
                    _defaultSamplerSettings = settingsField.GetValue(_llamaBotClient);
                    _samplerSettingsType = typeField.GetValue(_llamaBotClient) as Type;
                }

                if (_defaultSamplerSettings == null || _samplerSettingsType == null)
                {
                    // If we can't get settings, just cancel initialization
                    return Task.FromResult(InitializationResult.Cancel());
                }
            }
            catch
            {
                return Task.FromResult(InitializationResult.Cancel());
            }

            return Task.FromResult(InitializationResult.Success());
        }

        private static object? ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(float))
            {
                return float.Parse(value);
            }

            if (targetType == typeof(double))
            {
                return double.Parse(value);
            }

            if (targetType == typeof(int))
            {
                return int.Parse(value);
            }

            if (targetType == typeof(bool))
            {
                return bool.Parse(value);
            }

            if (targetType == typeof(string))
            {
                return value;
            }

            throw new NotSupportedException($"Cannot convert to type {targetType.Name}");
        }

        /// <summary>
        /// Gets all properties that can be set via the /setting command.
        /// Excludes read-only properties and those with [JsonIgnore].
        /// </summary>
        private static IEnumerable<PropertyInfo> GetSettableProperties(Type type)
        {
            return type.GetProperties().Where(IsSettableProperty);
        }

        /// <summary>
        /// Checks if a property can be set via the /setting command.
        /// Must have a setter, not have [JsonIgnore], and be a supported primitive type.
        /// </summary>
        private static bool IsSettableProperty(PropertyInfo prop)
        {
            if (!prop.CanWrite)
            {
                return false;
            }

            // Exclude properties marked with JsonIgnore (runtime state)
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            {
                return false;
            }

            // Only include types we can actually parse
            if (!IsSupportedType(prop.PropertyType))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a type is supported for setting via the /setting command.
        /// </summary>
        private static bool IsSupportedType(Type type)
        {
            return type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(int) ||
                   type == typeof(bool) ||
                   type == typeof(string);
        }

        private static string FormatSettings(object settings)
        {
            StringBuilder sb = new();
            sb.AppendLine("**Current Sampler Settings:**");

            foreach (PropertyInfo prop in GetSettableProperties(settings.GetType()))
            {
                object? value = prop.GetValue(settings);
                sb.AppendLine($"• **{prop.Name}**: {value}");
            }

            return sb.ToString();
        }
    }
}
