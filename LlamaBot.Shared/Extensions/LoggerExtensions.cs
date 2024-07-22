using LlamaBot.Shared.Interfaces;

namespace LlamaBot.Shared.Extensions
{
    public static class LoggerExtensions
    {
        public static void LogDebug(this ILogger logger, string message)
        {
            logger.Log(message, LogLevel.Debug);
        }

        public static void LogError(this ILogger logger, string message)
        {
            logger.Log(message, LogLevel.Error);
        }

        public static void LogError(this ILogger logger, Exception ex)
        {
            logger.Log(ex.ToString(), LogLevel.Error);
        }

        public static void LogFatal(this ILogger logger, string message)
        {
            logger.Log(message, LogLevel.Fatal);
        }

        public static void LogInfo(this ILogger logger, string message)
        {
            logger.Log(message, LogLevel.Info);
        }

        public static void LogTrace(this ILogger logger, string message)
        {
            logger.Log(message, LogLevel.Trace);
        }

        public static void LogWarn(this ILogger logger, string message)
        {
            logger.Log(message, LogLevel.Warn);
        }
    }
}