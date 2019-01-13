using KrogerScrape.Client;

namespace KrogerScrape.Logic
{
    public class StopOrphansCommandLogic
    {
        private readonly KrogerClientFactory _krogerClientFactory;

        public StopOrphansCommandLogic(KrogerClientFactory krogerClientFactory)
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
