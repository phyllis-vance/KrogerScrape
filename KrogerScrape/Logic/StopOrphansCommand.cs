using System.Threading.Tasks;
using KrogerScrape.Client;

namespace KrogerScrape.Logic
{
    public class StopOrphansCommand
    {
        private readonly KrogerClientFactory _krogerClientFactory;

        public StopOrphansCommand(KrogerClientFactory krogerClientFactory)
        {
            _krogerClientFactory = krogerClientFactory;
        }

        public Task ExecuteAsync()
        {
            using (var krogerClient = _krogerClientFactory.Create())
            {
                krogerClient.KillOrphanBrowsers();
            }

            return Task.CompletedTask;
        }
    }
}
