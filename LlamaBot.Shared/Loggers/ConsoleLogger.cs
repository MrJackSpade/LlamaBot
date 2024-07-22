using LlamaBot.Shared.Interfaces;

namespace LlamaBot.Shared.Loggers
{
    public class ConsoleLogger : ILogger
    {
        private static readonly object _consoleLock = new();

        public void Log(string message, LogLevel level)
        {
            lock (_consoleLock)
            {
                // Save the current foreground color so it can be restored after logging.
                ConsoleColor originalColor = Console.ForegroundColor;

                // Set the console text color based on the log level.
                Console.ForegroundColor = level switch
                {
                    LogLevel.Trace => ConsoleColor.Gray,
                    LogLevel.Debug => ConsoleColor.Blue,
                    LogLevel.Info => ConsoleColor.Green,
                    LogLevel.Warn => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Fatal => ConsoleColor.DarkRed,
                    _ => ConsoleColor.White,// Default to white if unknown level
                };

                // Log the message with the specified color.
                Console.WriteLine($"{level}: {message}");

                // Restore the original color.
                Console.ForegroundColor = originalColor;
            }
        }
    }
}