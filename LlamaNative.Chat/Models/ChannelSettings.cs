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

            return toReturn;
        }

        public Dictionary<string, string> Thoughts { get; set; } = [];

        public string? Prompt { get; set; }

        public void SetThoughts(string username, string thoughts)
        {
            username ??= string.Empty;

            if (!Thoughts.TryAdd(username, thoughts))
            {
                Thoughts[username] = thoughts;
            }
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
    }
}