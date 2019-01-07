namespace KrogerScrape.Settings
{
    public class KrogerScrapeSettings : IKrogerScrapeSettings
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string DatabasePath { get; set; }
        public bool Debug { get; set; }
        public string DownloadsPath { get; set; }
        public bool RefetchReceipts { get; set; }
    }
}
