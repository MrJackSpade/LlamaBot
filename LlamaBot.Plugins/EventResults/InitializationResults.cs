namespace LlamaBot.Plugins.EventResults
{
    public struct InitializationResult
    {
        public bool IsCancel { get; set; }

        public bool IsError { get; set; }

        public bool IsSuccess => !IsCancel && !IsError;

        public static InitializationResult Cancel()
        { return new InitializationResult() { IsCancel = true }; }

        public static InitializationResult Success()
        { return new InitializationResult(); }

        public static Task<InitializationResult> SuccessAsync()
        { return Task.FromResult(new InitializationResult()); }
    }
}