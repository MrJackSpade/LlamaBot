using Discord;
using LlamaBot.Discord.Attributes;
using LlamaBot.Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;

namespace LlamaBot.Discord.Extensions
{
    internal static class SlashCommandBuilderExtensions
    {
        public static void AddOption(this SlashCommandBuilder builder, SlashCommandOption option)
        {
            SlashCommandOptionBuilder optionBuilder = new();

            optionBuilder = optionBuilder.WithName(option.Name);
            optionBuilder = optionBuilder.WithDescription(option.Description);

            optionBuilder = optionBuilder.WithRequired(option.Required);
            optionBuilder = optionBuilder.WithType(option.Type);

            if (option.Choices.Any())
            {
                foreach (string choice in option.Choices)
                {
                    optionBuilder.AddChoice(choice, choice);
                }
            }

            builder.AddOption(optionBuilder);
        }

        public static bool TryAddOption(this SlashCommandBuilder commandBuilder, PropertyInfo property)
        {
            if (property.DeclaringType != property.ReflectedType ||
                   property.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null)
            {
                return false;
            }

            SlashCommandOptionBuilder optionBuilder = new();

            string oname = property.Name.ToLower();
            string odescription = property.Name;

            if (property.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute d)
            {
                if (!string.IsNullOrWhiteSpace(d.Name))
                {
                    oname = d.Name.ToLower();
                }

                if (!string.IsNullOrWhiteSpace(d.Description))
                {
                    odescription = d.Description;
                }
            }

            optionBuilder = optionBuilder.WithName(oname);
            optionBuilder = optionBuilder.WithDescription(odescription);

            if (property.GetCustomAttribute<RequiredAttribute>() is RequiredAttribute r)
            {
                optionBuilder = optionBuilder.WithRequired(true);
            }

            if (property.GetCustomAttribute<OptionTypeAttribute>() is OptionTypeAttribute ota)
            {
                optionBuilder.WithType(ota.Type);
            }
            else if (property.PropertyType.IsEnum)
            {
                optionBuilder = optionBuilder.WithType(ApplicationCommandOptionType.String);

                foreach (Enum value in Enum.GetValues(property.PropertyType))
                {
                    optionBuilder.AddChoice(value.ToString(), value.ToString());
                }
            }
            else
            {
                optionBuilder = property.PropertyType.Name switch
                {
                    "List`1" or
                    nameof(String) or
                    nameof(Int64) or
                    nameof(UInt64) => optionBuilder.WithType(ApplicationCommandOptionType.String),
                    nameof(Int32) or
                    nameof(UInt32) => optionBuilder.WithType(ApplicationCommandOptionType.Integer),
                    nameof(Decimal) => optionBuilder.WithType(ApplicationCommandOptionType.Number),
                    nameof(Boolean) => optionBuilder.WithType(ApplicationCommandOptionType.Boolean),
                    nameof(CommandAttachment) => optionBuilder.WithType(ApplicationCommandOptionType.Attachment),
                    _ => throw new NotImplementedException(),
                };
            }

            commandBuilder.AddOption(optionBuilder);

            return true;
        }
    }
}