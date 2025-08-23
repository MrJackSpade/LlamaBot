namespace LlamaBotAutomated.Discord.Exceptions
{
    internal class CommandPropertyValidationException(string propertyName, string message) : CommandValidationException(message)
    {
        public string PropertyName { get; set; } = propertyName;
    }
}