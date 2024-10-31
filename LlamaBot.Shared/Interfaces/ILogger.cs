namespace LlamaBot.Shared.Interfaces
{
    public enum LogLevel
    {
        Trace,

        Debug,

        Info,

        Warn,

        Error,

        Fatal
    }

    public interface ILogger
    {
        void Log(string message, LogLevel level);
    }
}