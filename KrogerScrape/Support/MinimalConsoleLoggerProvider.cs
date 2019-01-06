using Microsoft.Extensions.Logging;

namespace KrogerScrape.Support
{
    public class MinimalConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new MinimalConsoleLogger();

        public void Dispose()
        {
        }
    }
}
