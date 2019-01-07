using System;
using System.Threading;
using System.Threading.Tasks;
using KrogerScrape.Client;
using KrogerScrape.Entities;
using KrogerScrape.Logic;
using KrogerScrape.Settings;
using KrogerScrape.Support;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KrogerScrape
{

    public class Program
    {
        public static int Main(string[] args)
        {
            ILogger logger = new MinimalConsoleLogger();
            try
            {
                using (var serviceProvider = GetServiceProvider())
                {
                    logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                    var token = GetCancellationToken(logger);

                    var app = new CommandLineApplication();

                    ConfigureApplication(app, serviceProvider, logger, token);

                    return app.Execute(args);
                }
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

        private static CancellationToken GetCancellationToken(ILogger logger)
        {
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

            var token = cts.Token;
            return token;
        }

        private static ServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(lb => lb
                .AddProvider(new MinimalConsoleLoggerProvider())
                .SetMinimumLevel(LogLevel.Trace)
                .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));

            serviceCollection.AddTransient(sp => new EntityContextFactory(() => sp.GetRequiredService<IEntityContext>()));
            serviceCollection.AddDbContext<SqliteEntityContext>(
                (sp, ob) =>
                {
                    var settings = sp.GetRequiredService<IKrogerScrapeSettings>();

                    var builder = new SqliteConnectionStringBuilder();
                    builder.DataSource = settings.DatabasePath;

                    ob.UseSqlite(builder.ConnectionString);
                },
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Transient);
            serviceCollection.AddTransient<IEntityContext>(sp => sp.GetRequiredService<SqliteEntityContext>());

            serviceCollection.AddTransient<EntityRepository>();
            serviceCollection.AddTransient<KrogerClient>();
            serviceCollection.AddTransient(sp => new KrogerClientFactory(() => sp.GetRequiredService<KrogerClient>()));
            serviceCollection.AddTransient<ScrapeCommand>();
            serviceCollection.AddTransient<StopOrphansCommand>();

            serviceCollection.AddSingleton<KrogerScrapeSettingsFactory>();
            serviceCollection.AddTransient(sp => sp.GetRequiredService<KrogerScrapeSettingsFactory>().Create());
            serviceCollection.AddTransient<IKrogerClientSettings>(sp => sp.GetRequiredService<KrogerScrapeSettingsFactory>().Create());

            return serviceCollection.BuildServiceProvider();
        }

        private static void ConfigureApplication(
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
            var versionOption = app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);
            versionOption.Description = "Show version information.";
            app.CustomHelpOption();

            app.OnExecute(() =>
            {
                app.ShowHelp(usePager: false);
                logger.LogWarning("Specify a command to continue.");
                return 1;
            });

            app.Command("scrape", c => ConfigureScrapeCommand(c, serviceProvider, logger, token));
            app.Command("stop-orphans", c => ConfigureStopOrphansCommand(c, serviceProvider, logger));
        }

        private static void ConfigureScrapeCommand(
            CommandLineApplication app,
            IServiceProvider serviceProvider,
            ILogger logger,
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

                return await ExecuteScrapeCommandAsync(app.Name, serviceProvider, logger, token);
            });
        }

        private static async Task<int> ExecuteScrapeCommandAsync(
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

            var command = serviceProvider.GetRequiredService<ScrapeCommand>();

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

        private static void ConfigureStopOrphansCommand(
            CommandLineApplication app,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            app.Description = "Stop orphan Chromium processes.";

            var downloadsPathOption = app.DownloadsPathOption();
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

            var command = serviceProvider.GetRequiredService<StopOrphansCommand>();

            logger.LogDebug("Executing the {CommandName} command.", commandName);

            command.Execute();

            logger.LogInformation("The {CommandName} command has completed.", commandName);

            return 0;
        }
    }
}
