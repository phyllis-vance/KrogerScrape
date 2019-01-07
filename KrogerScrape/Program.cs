using System;
using System.Threading;
using KrogerScrape.Client;
using KrogerScrape.Entities;
using KrogerScrape.Logic;
using KrogerScrape.Support;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace KrogerScrape
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var loggerFactory = new LoggerFactory(
                providers: new[] { new MinimalConsoleLoggerProvider() },
                filterOptions: new LoggerFilterOptions { MinLevel = LogLevel.Trace });
            var logger = loggerFactory.CreateLogger<Program>();

            var cts = new CancellationTokenSource();
            var cancelled = 0;
            var cancelledSemaphore = new SemaphoreSlim(0);
            Console.CancelKeyPress += (sender, e) =>
            {
                if (Interlocked.CompareExchange(ref cancelled, 1, 0) == 1)
                {
                    e.Cancel = false;
                }
                else
                {
                    logger.LogWarning("Cancelling. Press Ctrl + C again to terminate.");
                    e.Cancel = true;
                    cts.Cancel();
                }
            };

            var app = new CommandLineApplication();

            ConfigureApplication(app, loggerFactory, logger, cts.Token);

            try
            {
                return app.Execute(args);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Cancelled.");
                return 1;
            }
            catch (CommandParsingException ex)
            {
                logger.LogError(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception.");
                return 1;
            }
        }

        private static void ConfigureApplication(
            CommandLineApplication app,
            LoggerFactory loggerFactory,
            ILogger<Program> logger,
            CancellationToken token)
        {
            app.FullName = "KrogerScrape";
            app.Name = app.FullName;
            app.Description = "Fetch receipt data from Kroger.com.";

            app.ValueParsers.Add(new MarkerObjectValueParser<DownloadsPath>());
            app.ValueParsers.Add(new MarkerObjectValueParser<DatabasePath>());
            var versionOption = app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);
            versionOption.Description = "Show version information.";
            app.CustomHelpOption();

            app.OnExecute(() =>
            {
                app.ShowHelp(usePager: false);
                logger.LogWarning("Specify a command to continue.");
                return 1;
            });

            app.Command("scrape", c => ConfigureScrapeCommand(c, loggerFactory, logger, token));
            app.Command("stop-orphans", c => ConfigureStopOrphansCommand(c, loggerFactory, logger));
        }

        private static void ConfigureScrapeCommand(
            CommandLineApplication app,
            ILoggerFactory loggerFactory,
            ILogger<Program> logger,
            CancellationToken token)
        {
            app.Description = "Log in to Kroger.com and download all available purchase history.";

            var emailOption = app.Option(
                "-e|--email <EMAIL>",
                "Required. The email address for your Kroger account.",
                CommandOptionType.SingleValue,
                o => o.IsRequired().Accepts(a => a.EmailAddress()));
            var passwordOption = app.Option(
                "-p|--password <PASSWORD>",
                "The password for your Kroger account. Defaults to acquiring it interactively.",
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
            app.CustomHelpOption();

            app.OnExecute(async () =>
            {
                var email = emailOption.Value().Trim();
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

                logger.LogInformation("Initializing the {CommandName} command.", app.Name);

                if (refetchOption.HasValue())
                {
                    logger.LogInformation("Both new receipts and receipts that have already been fetched will be fetched.");
                }
                else
                {
                    logger.LogInformation("Only new receipts will be fetched.");
                }

                if (debugOption.HasValue())
                {
                    logger.LogInformation("Debug mode is enabled.");
                }

                var downloadsPath = downloadsPathOption.GetDownloadsPath();
                logger.LogDebug($"Using downloads path:{Environment.NewLine}{{DownloadsPath}}", downloadsPath);

                var databasePath = databasePathOption.GetDatabasePath();
                logger.LogDebug($"Using database path:{Environment.NewLine}{{DatabasePath}}", databasePath);

                var entityContextFactory = new EntityContextFactory(databasePath, loggerFactory);
                using (var entityContext = entityContextFactory.Get())
                {
                    await entityContext.MigrateAsync(token);
                }

                var entityRepository = new EntityRepository(entityContextFactory);

                var krogerClientFactory = new KrogerClientFactory(
                    downloadsPath,
                    debugOption.HasValue(),
                    loggerFactory);

                var scrapeCommand = new ScrapeCommand(
                    entityRepository,
                    krogerClientFactory,
                    loggerFactory.CreateLogger<ScrapeCommand>());

                var success = await scrapeCommand.ExecuteAsync(
                    email,
                    password,
                    refetchOption.HasValue(),
                    token);

                if (success)
                {
                    logger.LogInformation("The {CommandName} command has completed successfully.", app.Name);
                    return 0;
                }
                else
                {
                    logger.LogInformation("The {CommandName} command has failed.", app.Name);
                    return 1;
                }
            });
        }

        private static void ConfigureStopOrphansCommand(
            CommandLineApplication app,
            ILoggerFactory loggerFactory,
            ILogger<Program> logger)
        {
            app.Description = "Stop orphan Chromium processes.";

            var downloadsPathOption = app.DownloadsPathOption();
            app.CustomHelpOption();

            app.OnExecute(() =>
            {
                logger.LogInformation("Initializing the {CommandName} command.", app.Name);

                var downloadsPath = downloadsPathOption.GetDownloadsPath();
                logger.LogDebug($"Using downloads path:{Environment.NewLine}{{DownloadsPath}}", downloadsPath);

                var krogerClientFactory = new KrogerClientFactory(
                    downloadsPath,
                    debug: false,
                    loggerFactory: loggerFactory);

                var stopOrphansCommand = new StopOrphansCommand(krogerClientFactory);

                stopOrphansCommand.Execute();

                logger.LogInformation("The {CommandName} command has completed.", app.Name);

                return 0;
            });
        }
    }
}
