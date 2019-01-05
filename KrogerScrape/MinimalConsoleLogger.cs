using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace KrogerScrape
{
    public class MinimalConsoleLogger : ILogger
    {
        private static readonly object ConsoleLock = new object();
        private static IConsole Console = PhysicalConsole.Singleton;

        public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            ConsoleColor? color = null;
            if (logLevel <= LogLevel.Debug)
            {
                color = ConsoleColor.Gray;
            }
            else if (logLevel == LogLevel.Warning)
            {
                color = ConsoleColor.Yellow;
            }
            else if (logLevel >= LogLevel.Error)
            {
                color = ConsoleColor.Red;
            }

            var message = formatter(state, exception);

            lock (ConsoleLock)
            {
                if (color.HasValue)
                {
                    Console.ForegroundColor = color.Value;
                }

                Console.WriteLine(message);
                if (exception != null)
                {
                    Console.WriteLine(exception);
                }

                Console.ResetColor();
            }
        }

        private class NullDisposable : IDisposable
        {
            public static NullDisposable Instance { get; } = new NullDisposable();

            public void Dispose()
            {
            }
        }
    }
}
