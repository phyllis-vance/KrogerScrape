using System;
using System.Threading;
using KrogerScrape.Logic;
using KrogerScrape.Settings;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KrogerScrape
{
    public static class StopOrphansCommand
    {
        public static void Configure(
            CommandLineApplication app,
            IServiceProvider serviceProvider,
            ILogger logger,
            CancellationToken token)
        {
            app.Description = "Stop orphan Chromium processes.";

            var downloadsPathOption = app.DownloadsPathOption();
            app.LogLevelOption();
            app.CustomHelpOption();

            app.OnExecute(() =>
            {
                var settingsFactory = serviceProvider.GetRequiredService<KrogerScrapeSettingsFactory>();
                settingsFactory.Initialize(new KrogerScrapeSettings
                {
                    DownloadsPath = downloadsPathOption.GetDownloadsPath(),
                });

                return ExecuteStopOrphansCommand(app.Name, serviceProvider, logger);
            });
        }

        private static int ExecuteStopOrphansCommand(string commandName, IServiceProvider serviceProvider, ILogger logger)
        {
            logger.LogDebug("Initializing the {CommandName} command.", commandName);

            var settings = serviceProvider.GetRequiredService<IKrogerScrapeSettings>();
            logger.LogDebug($"Using downloads path:{Environment.NewLine}{{DownloadsPath}}", settings.DownloadsPath);

            var command = serviceProvider.GetRequiredService<StopOrphansCommandLogic>();

            logger.LogDebug("Executing the {CommandName} command.", commandName);

            command.Execute();

            logger.LogInformation("The {CommandName} command has completed.", commandName);

            return 0;
        }
    }
}
