using Discord;

namespace LlamaBot.Discord.Attributes
{
    internal class OptionTypeAttribute : Attribute
    {
        public OptionTypeAttribute(ApplicationCommandOptionType type)
        {
            Type = type;
        }

        public ApplicationCommandOptionType Type { get; private set; }
    }
}