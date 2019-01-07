using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace KrogerScrape.Settings
{
    public static class SettingsExtensionMethods
    {
        public const string LogLevelLongOption = "--log-level";
        public const LogLevel DefaultLogLevel = LogLevel.Information;

        private static string GetDefaultDatabasePath()
        {
            return Path.Combine(GetDefaultBaseDirectory(), "KrogerScrape.sqlite3");
        }

        private static string GetDefaultDownloadsPath()
        {
            return Path.Combine(GetDefaultBaseDirectory(), "Downloads");
        }

        private static string GetDefaultBaseDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        public static CommandOption<LogLevel> LogLevelOption(this CommandLineApplication app)
        {
            return app.Option<LogLevel>(
                $"{LogLevelLongOption} <LEVEL>",
                $"The minimum log level to show.{Environment.NewLine}" +
                $"Options: {string.Join(", ", Enum.GetNames(typeof(LogLevel)))}{Environment.NewLine}" +
                $"Default: {DefaultLogLevel}",
                CommandOptionType.SingleValue);
        }

        public static LogLevel GetLogLevel(this CommandOption<LogLevel> option)
        {
            return option.HasValue() ? option.ParsedValue : DefaultLogLevel;
        }

        public static CommandOption<DatabasePath> DatabasePathOption(this CommandLineApplication app)
        {
            return app.Option<DatabasePath>(
                "--database-path <PATH>",
                $"The path to the database path.{Environment.NewLine}" +
                $"Default: {GetDefaultDatabasePath()}",
                CommandOptionType.SingleValue);
        }

        public static CommandOption<DownloadsPath> DownloadsPathOption(this CommandLineApplication app)
        {
            return app.Option<DownloadsPath>(
                "--downloads-path <DIR>",
                $"The path to the where downloads (e.g. Chromium) should go.{Environment.NewLine}" +
                $"Default: {GetDefaultDownloadsPath()}",
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
