using Microsoft.Extensions.Logging;

namespace KrogerScrape.Client
{
    public class KrogerClientFactory
    {
        private readonly string _downloadsPath;
        private readonly bool _debug;
        private readonly ILoggerFactory _loggerFactory;

        public KrogerClientFactory(
            string downloadsPath,
            bool debug,
            ILoggerFactory loggerFactory)
        {
            _downloadsPath = downloadsPath;
            _debug = debug;
            _loggerFactory = loggerFactory;
        }

        public KrogerClient Create()
        {
            return new KrogerClient(
                _downloadsPath,
                _debug,
                _loggerFactory.CreateLogger<KrogerClient>());
        }
    }
}
