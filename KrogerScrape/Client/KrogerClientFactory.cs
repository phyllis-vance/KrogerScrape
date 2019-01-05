using Microsoft.Extensions.Logging;

namespace KrogerScrape.Client
{
    public class KrogerClientFactory
    {
        private readonly string _downloadsPath;
        private readonly ILoggerFactory _loggerFactory;

        public KrogerClientFactory(
            string downloadsPath,
            ILoggerFactory loggerFactory)
        {
            _downloadsPath = downloadsPath;
            _loggerFactory = loggerFactory;
        }

        public KrogerClient Create()
        {
            return new KrogerClient(
                _downloadsPath,
                _loggerFactory.CreateLogger<KrogerClient>());
        }
    }
}
