using Discord;

namespace LlamaBotAutomated.Discord.Attributes
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