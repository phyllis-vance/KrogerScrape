using System;
using System.Threading;
using System.Threading.Tasks;
using KrogerScrape.Entities;
using KrogerScrape.Logic;
using KrogerScrape.Settings;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KrogerScrape
{
    public static class ScrapeCommand
    {
        public static void Configure(
            CommandLineApplication app,
            IServiceProvider serviceProvider,
            ILogger logger,
            CancellationToken token)
        {
            app.Description = "Log in to Kroger.com and download all available purchase history.";

            var emailOption = app.EmailOption();
            var passwordOption = app.Option(
                "-p|--password <PASSWORD>",
                $"The password for your Kroger account.{Environment.NewLine}" +
                $"If not provided as an option, the password will be acquired interactively.",
                CommandOptionType.SingleValue);
            var refetchOption = app.Option(
                "--refetch",
                "Fetch receipts that have already been fetched.",
                CommandOptionType.NoValue);
            var debugOption = app.Option(
                "--debug",
                "Debug problems by showing diagnostic information.",
                CommandOptionType.NoValue);
            var downloadsPathOption = app.DownloadsPathOption();
            var databasePathOption = app.DatabasePathOption();
            app.LogLevelOption();
            app.CustomHelpOption();

            app.OnExecute(async () =>
            {
                string password;
                if (passwordOption.HasValue())
                {
                    password = passwordOption.Value();
                }
                else
                {
                    password = Prompt.GetPassword("Password:");
                }

                if (string.IsNullOrEmpty(password))
                {
                    logger.LogError(
                        "The password is required, either by using the --{LongName} option or by providing it interactively.",
                        passwordOption.LongName);
                    return 1;
                }

                var settingsFactory = serviceProvider.GetRequiredService<KrogerScrapeSettingsFactory>();
                settingsFactory.Initialize(new KrogerScrapeSettings
                {
                    Email = emailOption.Value().Trim(),
                    Password = password,
                    Debug = debugOption.HasValue(),
                    RefetchReceipts = refetchOption.HasValue(),
                    DatabasePath = databasePathOption.GetDatabasePath(),
                    DownloadsPath = downloadsPathOption.GetDownloadsPath(),
                });

                return await ExecuteAsync(app.Name, serviceProvider, logger, token);
            });
        }

        private static async Task<int> ExecuteAsync(
            string commandName,
            IServiceProvider serviceProvider,
            ILogger logger,
            CancellationToken token)
        {
            logger.LogDebug("Initializing the {CommandName} command.", commandName);

            var settings = serviceProvider.GetRequiredService<IKrogerScrapeSettings>();
            if (settings.RefetchReceipts)
            {
                logger.LogInformation("Both new receipts and receipts that have already been fetched will be fetched.");
            }
            else
            {
                logger.LogInformation("Only new receipts will be fetched.");
            }

            if (settings.Debug)
            {
                logger.LogInformation("Debug mode is enabled.");
            }

            logger.LogDebug($"Using downloads path:{Environment.NewLine}{{DownloadsPath}}", settings.DownloadsPath);
            logger.LogDebug($"Using database path:{Environment.NewLine}{{DatabasePath}}", settings.DatabasePath);

            var entityContextFactory = serviceProvider.GetRequiredService<EntityContextFactory>();
            using (var entityContext = entityContextFactory.Create())
            {
                await entityContext.MigrateAsync(token);
            }

            var command = serviceProvider.GetRequiredService<ScrapeCommandLogic>();

            logger.LogDebug("Executing the {CommandName} command.", commandName);

            var success = await command.ExecuteAsync(token);

            if (success)
            {
                logger.LogInformation("The {CommandName} command has completed successfully.", commandName);
                return 0;
            }
            else
            {
                logger.LogError("The {CommandName} command has failed.", commandName);
                return 1;
            }
        }
    }
}
