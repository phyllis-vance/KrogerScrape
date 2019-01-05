using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace KrogerScrape
{
    public static class ExtensionMethods
    {
        private static string GetDefaultDatabasePath()
        {
            return Path.Combine(GetApplicationDirectiry(), "KrogerScrape.sqlite3");
        }

        private static string GetDefaultDownloadsPath()
        {
            return Path.Combine(GetApplicationDirectiry(), "Downloads");
        }

        private static string GetApplicationDirectiry()
        {
            return Path.GetDirectoryName(typeof(Program).Assembly.Location);
        }

        public static CommandOption<DatabasePath> DatabasePathOption(this CommandLineApplication app)
        {
            return app.Option<DatabasePath>(
                "--database-path <PATH>",
                $"The path to the database path. Defaults to the application directory ({GetDefaultDatabasePath()})",
                CommandOptionType.SingleValue);
        }

        public static CommandOption<DownloadsPath> DownloadsPathOption(this CommandLineApplication app)
        {
            return app.Option<DownloadsPath>(
                "--downloads-path <DIR>",
                $"The path to the where downloads (e.g. Chromium) should go. Defaults to the application directory ({GetDefaultDownloadsPath()})",
                CommandOptionType.SingleValue);
        }

        public static CommandOption CustomHelpOption(this CommandLineApplication app)
        {
            var option = app.HelpOption();
            option.Description = "Show help information.";
            return option;
        }

        public static string GetDatabasePath(this CommandOption<DatabasePath> option)
        {
            if (option.HasValue() && !string.IsNullOrWhiteSpace(option.Value()))
            {
                return Path.GetFullPath(option.Value());
            }
            else
            {
                return GetDefaultDatabasePath();
            }
        }

        public static string GetDownloadsPath(this CommandOption<DownloadsPath> option)
        {
            if (option.HasValue() && !string.IsNullOrWhiteSpace(option.Value()))
            {
                return Path.GetFullPath(option.Value());
            }
            else
            {
                return GetDefaultDownloadsPath();
            }
        }
    }
}
