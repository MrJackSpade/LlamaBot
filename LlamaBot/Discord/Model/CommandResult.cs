namespace LlamaBot.Discord.Model
{
    public class CommandResult
    {
        private CommandResult(bool success, string message)
        {
            IsSuccess = success;
            Message = message;
        }

        public bool IsSuccess { get; }

        public string Message { get; }

        public static CommandResult Error(string message) => new(false, message);

        public static Task<CommandResult> ErrorAsync(string message) => Task.FromResult(Error(message));

        public static CommandResult Success() => new(true, string.Empty);

        public static CommandResult Success(string message) => new(true, message);

        public static Task<CommandResult> SuccessAsync(string message) => Task.FromResult(Success(message));

        public static Task<CommandResult> SuccessAsync() => Task.FromResult(Success());
    }
}