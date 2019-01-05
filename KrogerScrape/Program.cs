using System;
using KrogerScrape.Client;
using KrogerScrape.Entities;
using KrogerScrape.Logic;
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

            var app = new CommandLineApplication();

            ConfigureApplication(app, loggerFactory, logger);

            try
            {
                return app.Execute(args);
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

        private static void ConfigureApplication(CommandLineApplication app, LoggerFactory loggerFactory, ILogger<Program> logger)
        {
            app.Description = "Fetch receipt data from Kroger.com.";

            app.ValueParsers.Add(new MarkerObjectValueParser<DownloadsPath>());
            app.ValueParsers.Add(new MarkerObjectValueParser<DatabasePath>());
            app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);
            app.CustomHelpOption();

            app.OnExecute(() =>
            {
                app.ShowHelp();
                logger.LogWarning("Specify a command to continue.");
                return 1;
            });

            app.Command("scrape", c => ConfigureScrapeCommand(c, loggerFactory, logger));
        }

        private static void ConfigureScrapeCommand(CommandLineApplication app, ILoggerFactory loggerFactory, ILogger<Program> logger)
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
            var fetchAgainOption = app.Option(
                "-r|--refetch",
                "Fetch receipts that have already been fetched.",
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

                var downloadsPath = downloadsPathOption.GetDownloadsPath();
                logger.LogDebug($"Using downloads path:{Environment.NewLine}{{DownloadsPath}}", downloadsPath);

                var databasePath = databasePathOption.GetDatabasePath();
                logger.LogDebug($"Using database path:{Environment.NewLine}{{DatabasePath}}", databasePath);

                var entityContextFactory = new EntityContextFactory(databasePath, loggerFactory);
                using (var entityContext = await entityContextFactory.GetAsync())
                {
                    await entityContext.MigrateAsync();
                }

                var entityRepository = new EntityRepository(entityContextFactory);

                var krogerClientFactory = new KrogerClientFactory(
                    downloadsPath,
                    loggerFactory);

                var persistReceiptsCommand = new PersistReceiptsCommand(
                    entityRepository,
                    krogerClientFactory,
                    loggerFactory.CreateLogger<PersistReceiptsCommand>());

                await persistReceiptsCommand.ExecuteAsync(
                    email,
                    password,
                    fetchAgainOption.HasValue());

                return 0;
            });
        }
    }
}
