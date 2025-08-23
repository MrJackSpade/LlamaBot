using System.Text;

namespace LlamaBot
{
    public class ChannelSettings
    {
        public ChannelSettings()
        { }

        public ChannelSettings(string prompt)
        {
            this.Prompt = prompt;
        }

        public ChannelSettings(string prompt, string defaultThoughts)
        {
            this.Prompt = prompt;

            this.SetThoughts(string.Empty, defaultThoughts);
        }

        public Dictionary<string, string> Thoughts { get; set; } = new Dictionary<string, string>();

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

            if (Thoughts.TryGetValue(username, out var thought) && thought is not null)
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
                if (Thoughts.TryGetValue(username, out var thought) && thought is not null)
                {
                    toReturn.Append(thought);
                }
            }

            return toReturn.ToString();
        }
    }
}