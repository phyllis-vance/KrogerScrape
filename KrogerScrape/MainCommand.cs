using System;
using System.Threading;
using KrogerScrape.Settings;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace KrogerScrape
{
    public static class MainCommand
    {
        public static void Configure(
            CommandLineApplication app,
            IServiceProvider serviceProvider,
            ILogger logger,
            CancellationToken token)
        {
            app.FullName = "KrogerScrape";
            app.Name = app.FullName;
            app.Description = "Fetch receipt data from Kroger.com.";

            app.ValueParsers.Add(new MarkerObjectValueParser<DownloadsPath>());
            app.ValueParsers.Add(new MarkerObjectValueParser<DatabasePath>());
            app.LogLevelOption();
            var versionOption = app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);
            versionOption.Description = "Show version information.";
            app.CustomHelpOption();

            app.OnExecute(() =>
            {
                app.ShowHelp(usePager: false);
                logger.LogWarning("Specify a command to continue.");
                return 1;
            });

            app.Command("scrape", c => ScrapeCommand.Configure(c, serviceProvider, logger, token));
            app.Command("stop-orphans", c => StopOrphansCommand.Configure(c, serviceProvider, logger, token));
        }
    }
}
