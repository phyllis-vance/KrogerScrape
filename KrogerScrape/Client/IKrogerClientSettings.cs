namespace KrogerScrape.Client
{
    public interface IKrogerClientSettings
    {
        string Email { get; }
        string Password { get; }
        string DownloadsPath { get; }
        bool Debug { get; }
    }
}
