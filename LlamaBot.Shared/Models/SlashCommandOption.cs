using Discord;

namespace LlamaBot.Shared.Models
{
    public class SlashCommandOption
    {
        public SlashCommandOption(string name, string description, bool required, params string[] choices)
        {
            Name = name.ToLower();
            Description = description;
            Required = required;
            Choices = choices;
        }

        public string[] Choices { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Name { get; set; }

        public bool Required { get; set; }

        public ApplicationCommandOptionType Type { get; set; } = ApplicationCommandOptionType.String;
    }
}