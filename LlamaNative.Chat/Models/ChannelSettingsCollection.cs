using LlamaBot;
using System.Text.Json;

namespace LlamaNative.Chat.Models
{
    public class ChannelSettingsCollection
    {
        private const string CHANNEL_SETTINGS_DIR = "ChannelSettings";

        private readonly Dictionary<ulong, ChannelSettings?> _channels = [];

        public void AddOrUpdate(ulong channelId, ChannelSettings channelSettings)
        {
            _channels[channelId] = channelSettings;
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