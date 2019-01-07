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

        public void Execute()
        {
            using (var krogerClient = _krogerClientFactory.Create())
            {
                krogerClient.KillOrphanBrowsers();
            }
        }
    }
}
