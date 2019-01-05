using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace KrogerScrape
{
    public static class ExtensionMethods
    {
        private static string GetDefaultDatabasePath()
        {
            var applicationDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var defaultPath = Path.Combine(applicationDirectory, "KrogerScrape.sqlite3");
            return defaultPath;
        }

        public static CommandOption<DatabasePath> DatabasePathOption(this CommandLineApplication app)
        {
            return app.Option<DatabasePath>(
                "-db|--databasePath <PATH>",
                $"The path to the database path. Defaults to the application directory ({GetDefaultDatabasePath()})",
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
    }
}
