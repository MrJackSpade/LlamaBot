namespace LlamaBot.Shared.Models
{
    public class CommandResult
    {
        private CommandResult(bool success, string message)
        {
            IsSuccess = success;
            Message = message;
        }

        private CommandResult(byte[]? fileData, string? fileName)
        {
            IsSuccess = true;
            Message = string.Empty;
            FileData = fileData;
            FileName = fileName;
        }

        public byte[]? FileData { get; init; }

        public string? FileName { get; init; }

        public bool IsFile => FileData is not null;

        public bool IsSuccess { get; }

        public string Message { get; }

        public static CommandResult Error(string message)
        {
            return new(false, message);
        }

        public static Task<CommandResult> ErrorAsync(string message)
        {
            return Task.FromResult(Error(message));
        }

        public static CommandResult Success()
        {
            return new(true, string.Empty);
        }

        public static CommandResult Success(string message)
        {
            return new(true, message);
        }

        public static CommandResult Success(byte[] data, string fileName)
        {
            return new(data, fileName);
        }

        public static Task<CommandResult> SuccessAsync(string message)
        {
            return Task.FromResult(Success(message));
        }

        public static Task<CommandResult> SuccessAsync()
        {
            return Task.FromResult(Success());
        }
    }
}