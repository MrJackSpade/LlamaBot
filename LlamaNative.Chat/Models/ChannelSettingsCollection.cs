using LlamaBot;

namespace LlamaNative.Chat.Models
{
    public class ChannelSettingsCollection
    {
        private Dictionary<ulong, ChannelSettings?> _channels = [];

        private const string CHANNEL_SETTINGS_DIR = "ChannelSettings";

        public ChannelSettings? GetValue(ulong channelId)
        {
            if (!IsLoaded(channelId))
            {
                LoadSettings(channelId);
            }

            _channels.TryGetValue(channelId, out ChannelSettings? channelSettings);

            return channelSettings;
        }

        public void SetPrompt(ulong channelId, string prompt)
        {
            if (!IsLoaded(channelId))
            {
                LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                channelSettings = new ChannelSettings();
                _channels[channelId] = channelSettings;
            }

            channelSettings.Prompt = prompt;
        }

        public string? GetPrompt(ulong channelId)
        {
            if (!IsLoaded(channelId))
            {
                LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                return null;
            }

            return channelSettings.Prompt;
        }

        public void SetThoughts(ulong channelId, string username, string thoughts)
        {
            username ??= string.Empty;

            if (!IsLoaded(channelId))
            {
                LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                channelSettings = new ChannelSettings();
                _channels[channelId] = channelSettings;
            }

            channelSettings.SetThoughts(username, thoughts);
        }

        public string? GetUserThoughts(ulong channelId, string username)
        {
            username ??= string.Empty;

            if (!IsLoaded(channelId))
            {
                LoadSettings(channelId);
            }

            ChannelSettings? channelSettings = _channels[channelId];

            if (channelSettings == null)
            {
                return null;
            }

            return channelSettings.GetUserThoughts(username);
        }

        public bool IsLoaded(ulong channelId)
        {
            return _channels.ContainsKey(channelId);
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
                    string json = System.Text.Json.JsonSerializer.Serialize(channelSettings);

                    File.WriteAllText(path, json);
                }
            }
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
    }
}