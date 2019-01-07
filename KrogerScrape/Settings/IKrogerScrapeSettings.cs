using KrogerScrape.Client;

namespace KrogerScrape.Settings
{
    public interface IKrogerScrapeSettings : IKrogerClientSettings
    {
        string DatabasePath { get; }
        bool RefetchReceipts { get; }
    }
}
