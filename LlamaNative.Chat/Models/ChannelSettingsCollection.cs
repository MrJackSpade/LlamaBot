using LlamaBot;
using System.Text.Json;

namespace LlamaNative.Chat.Models
{
    public class ChannelSettingsCollection
    {
        private const string CHANNEL_SETTINGS_DIR = "ChannelSettings";

        private readonly Dictionary<ulong, ChannelSettings?> _channels = [];

        /// <summary>
        /// In-memory cache of deserialized sampler settings per channel.
        /// </summary>
        private readonly Dictionary<ulong, object> _samplerSettingsCache = [];

        public void AddOrUpdate(ulong channelId, ChannelSettings channelSettings)
        {
            _channels[channelId] = channelSettings;
        }

        public string? GetNameOverride(ulong channelId, ulong userId)
        {
            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                return null;
            }

            return channelSettings.GetNameOverride(userId);
        }

        public string? GetPrompt(ulong channelId)
        {
            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                return null;
            }

            return channelSettings.Prompt;
        }

        /// <summary>
        /// Gets the sampler settings for a channel. Returns from cache if available,
        /// otherwise loads from disk or returns a clone of the default settings.
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="defaultSettings">The default settings to clone if no channel-specific settings exist.</param>
        /// <param name="settingsType">The type to deserialize the settings as.</param>
        /// <returns>The sampler settings object for this channel.</returns>
        public object GetSamplerSettings(ulong channelId, object defaultSettings, Type settingsType)
        {
            // Check cache first
            if (_samplerSettingsCache.TryGetValue(channelId, out object? cached))
            {
                return cached;
            }

            // Try to load from disk
            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            ChannelSettings? cs = _channels.GetValueOrDefault(channelId);
            if (cs?.SamplerSettingsJson != null)
            {
                object? loaded = System.Text.Json.JsonSerializer.Deserialize(cs.SamplerSettingsJson, settingsType);
                if (loaded != null)
                {
                    _samplerSettingsCache[channelId] = loaded;
                    return loaded;
                }
            }

            // Clone default and cache
            string defaultJson = System.Text.Json.JsonSerializer.Serialize(defaultSettings);
            object cloned = System.Text.Json.JsonSerializer.Deserialize(defaultJson, settingsType)!;
            _samplerSettingsCache[channelId] = cloned;
            return cloned;
        }

        public string? GetUserThoughts(ulong channelId, string username)
        {
            username ??= string.Empty;

            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                return null;
            }

            return channelSettings.GetUserThoughts(username);
        }

        public ChannelSettings? GetValue(ulong channelId)
        {
            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            _channels.TryGetValue(channelId, out ChannelSettings? channelSettings);

            return channelSettings;
        }

        public bool IsLoaded(ulong channelId)
        {
            return _channels.ContainsKey(channelId);
        }

        public void LoadSettings(ulong channelId)
        {
            string path = Path.Combine(CHANNEL_SETTINGS_DIR, $"{channelId}.json");

            FileInfo fi = new(path);

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            string? json = null;
            ChannelSettings? channelSettings = null;

            if (fi.Exists)
            {
                json = File.ReadAllText(path);

                channelSettings = System.Text.Json.JsonSerializer.Deserialize<ChannelSettings>(json!);
            }

            if (!_channels.TryAdd(channelId, channelSettings))
            {
                _channels[channelId] = channelSettings;
            }
        }

        public void SaveSettings(ulong channelId)
        {
            string path = Path.Combine(CHANNEL_SETTINGS_DIR, $"{channelId}.json");

            FileInfo fi = new(path);

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            if (_channels.TryGetValue(channelId, out ChannelSettings? channelSettings))
            {
                if (channelSettings != null)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(channelSettings, new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(path, json);
                }
            }
        }

        public void SetNameOverride(ulong channelId, ulong userId, string name)
        {
            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                channelSettings = new ChannelSettings();
                _channels[channelId] = channelSettings;
            }

            channelSettings.SetNameOverride(userId, name);
        }

        public void SetPrompt(ulong channelId, string prompt)
        {
            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                channelSettings = new ChannelSettings();
                _channels[channelId] = channelSettings;
            }

            channelSettings.Prompt = prompt;
        }

        /// <summary>
        /// Sets the sampler settings for a channel, updating both cache and persisted storage.
        /// </summary>
        public void SetSamplerSettings(ulong channelId, object settings)
        {
            _samplerSettingsCache[channelId] = settings;

            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];
            if (channelSettings == null)
            {
                channelSettings = new ChannelSettings();
                _channels[channelId] = channelSettings;
            }

            channelSettings.SamplerSettingsJson = System.Text.Json.JsonSerializer.Serialize(settings);
        }

        public void SetThoughts(ulong channelId, string username, string thoughts)
        {
            username ??= string.Empty;

            if (!this.IsLoaded(channelId))
            {
                this.LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                channelSettings = new ChannelSettings();
                _channels[channelId] = channelSettings;
            }

            channelSettings.SetThoughts(username, thoughts);
        }
    }
}