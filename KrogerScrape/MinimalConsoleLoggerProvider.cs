using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace KrogerScrape
{
    public class MinimalConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new MinimalConsoleLogger();

        public void Dispose()
        {
        }
    }
}
