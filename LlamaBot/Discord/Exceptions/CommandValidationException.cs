namespace LlamaBot.Discord.Exceptions
{
    internal class CommandValidationException(string message) : Exception(message)
    {
    }
}