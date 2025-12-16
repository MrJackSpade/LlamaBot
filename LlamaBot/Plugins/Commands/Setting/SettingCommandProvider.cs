using LlamaBot.Extensions;
using LlamaBot.Plugins.EventArgs;
using LlamaBot.Plugins.EventResults;
using LlamaBot.Plugins.Interfaces;
using LlamaBot.Shared.Models;
using LlamaNative.Chat.Models;
using System.Reflection;
using System.Text;

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

            // Find the property to modify (case-insensitive)
            PropertyInfo? property = _samplerSettingsType.GetProperties()
                .FirstOrDefault(p => p.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase));

            if (property == null)
            {
                StringBuilder validSettings = new();
                validSettings.AppendLine("Unknown setting. Valid settings:");
                foreach (PropertyInfo prop in _samplerSettingsType.GetProperties().Where(p => p.CanWrite))
                {
                    validSettings.AppendLine($"  • {prop.Name} ({prop.PropertyType.Name})");
                }
                return Task.FromResult(CommandResult.Error(validSettings.ToString()));
            }

            if (!property.CanWrite)
            {
                return Task.FromResult(CommandResult.Error($"Setting '{property.Name}' is read-only"));
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

        private static string FormatSettings(object settings)
        {
            StringBuilder sb = new();
            sb.AppendLine("**Current Sampler Settings:**");

            foreach (PropertyInfo prop in settings.GetType().GetProperties())
            {
                object? value = prop.GetValue(settings);
                sb.AppendLine($"• **{prop.Name}**: {value}");
            }

            return sb.ToString();
        }
    }
}