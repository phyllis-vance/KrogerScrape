using System;
using System.Linq;
using System.Threading;
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
                var minimumLogLevel = GetMinimumLogLevel(args, logger);
                using (var serviceProvider = GetServiceProvider(minimumLogLevel))
                {
                    logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                    var token = GetCancellationToken(logger);

                    var app = new CommandLineApplication();

                    MainCommand.Configure(app, serviceProvider, logger, token);

                    return app.Execute(args);
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogDebug(ex, "Cancelled.");
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

        private static LogLevel GetMinimumLogLevel(string[] args, ILogger logger)
        {
            var app = new CommandLineApplication();
            app.ThrowOnUnexpectedArgument = false;
            var logLevelOption = app.LogLevelOption();

            var skippedArgs = args
                .SkipWhile(a => !a.Contains(logLevelOption.LongName))
                .ToArray();

            try
            {
                app.Parse(skippedArgs);
                return logLevelOption.GetLogLevel();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "The log level failed to be parsed.");
                return SettingsExtensionMethods.DefaultLogLevel;
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

        private static ServiceProvider GetServiceProvider(LogLevel minimumLogLevel)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(lb => lb
                .AddProvider(new MinimalConsoleLoggerProvider())
                .SetMinimumLevel(minimumLogLevel)
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

            serviceCollection.AddTransient<Deserializer>();
            serviceCollection.AddTransient<EntityRepository>();
            serviceCollection.AddTransient<KrogerClient>();
            serviceCollection.AddTransient(sp => new KrogerClientFactory(() => sp.GetRequiredService<KrogerClient>()));

            serviceCollection.AddTransient<ScrapeCommandLogic>();
            serviceCollection.AddTransient<StopOrphansCommandLogic>();
            serviceCollection.AddTransient<JsonCommandLogic>();

            serviceCollection.AddSingleton<KrogerScrapeSettingsFactory>();
            serviceCollection.AddTransient(sp => sp.GetRequiredService<KrogerScrapeSettingsFactory>().Create());
            serviceCollection.AddTransient<IKrogerClientSettings>(sp => sp.GetRequiredService<KrogerScrapeSettingsFactory>().Create());

            return serviceCollection.BuildServiceProvider();
        }
    }
}
