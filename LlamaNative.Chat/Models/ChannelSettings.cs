using System.Text;

namespace LlamaBot
{
    public class ChannelSettings
    {
        public ChannelSettings()
        { }

        public ChannelSettings(string prompt)
        {
            Prompt = prompt;
        }

        public ChannelSettings(string prompt, string defaultThoughts)
        {
            Prompt = prompt;

            this.SetThoughts(string.Empty, defaultThoughts);
        }

        public Dictionary<ulong, string> NameOverrides { get; set; } = [];

        public string? Prompt { get; set; }

        /// <summary>
        /// JSON-serialized sampler settings for this channel. Must be deserialized with the appropriate settings type.
        /// </summary>
        public string? SamplerSettingsJson { get; set; }

        public Dictionary<string, string> Thoughts { get; set; } = [];

        public ChannelSettings Clone()
        {
            ChannelSettings toReturn = new()
            {
                Prompt = Prompt
            };

            foreach (KeyValuePair<string, string> thought in Thoughts)
            {
                toReturn.SetThoughts(thought.Key, thought.Value);
            }

            foreach (KeyValuePair<ulong, string> overrideName in NameOverrides)
            {
                toReturn.SetNameOverride(overrideName.Key, overrideName.Value);
            }

            return toReturn;
        }

        public string? GetFullThoughts(string username)
        {
            username ??= string.Empty;

            StringBuilder toReturn = new();

            if (Thoughts.TryGetValue("", out string? systemThought) && systemThought is not null)
            {
                toReturn.AppendLine(systemThought);
            }

            if (username != string.Empty)
            {
                if (Thoughts.TryGetValue(username, out string? thought) && thought is not null)
                {
                    toReturn.Append(thought);
                }
            }

            return toReturn.ToString();
        }

        public string? GetNameOverride(ulong userId)
        {
            if (NameOverrides.TryGetValue(userId, out string? name) && name is not null)
            {
                return name;
            }

            return null;
        }

        public string? GetUserThoughts(string username)
        {
            username ??= string.Empty;

            if (Thoughts.TryGetValue(username, out string? thought) && thought is not null)
            {
                return thought;
            }

            return null;
        }

        public void SetNameOverride(ulong userId, string name)
        {
            if (!NameOverrides.TryAdd(userId, name))
            {
                NameOverrides[userId] = name;
            }
        }

        public void SetThoughts(string username, string thoughts)
        {
            username ??= string.Empty;

            if (!Thoughts.TryAdd(username, thoughts))
            {
                Thoughts[username] = thoughts;
            }
        }
    }
}