using Microsoft.Extensions.Logging;

namespace KrogerScrape.Client
{
    public class KrogerClientFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public KrogerClientFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public KrogerClient Create()
        {
            return new KrogerClient(_loggerFactory.CreateLogger<KrogerClient>());
        }
    }
}
