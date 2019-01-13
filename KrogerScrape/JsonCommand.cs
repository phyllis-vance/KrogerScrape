using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using KrogerScrape.Entities;
using KrogerScrape.Logic;
using KrogerScrape.Settings;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KrogerScrape
{
    public static class JsonCommand
    {

        public static void Configure(
            CommandLineApplication app,
            IServiceProvider serviceProvider,
            ILogger logger,
            CancellationToken token)
        {
            app.Description = "Output data already fetched from Kroger.com as JSON.";

            var emailOption = app.EmailOption();
            var jsonPathOption = app.Option(
                "--jsonpath <PATH>",
                "Optional. The JSONPath to apply to the receipts.",
                CommandOptionType.SingleValue,
                o => o.Accepts().Use(new JsonPathValidator()));
            var minTransactionDateOption = app.Option(
                "--min-transaction-date <DATE>",
                "Optional. The minimum, inclusive transaction date. Use format YYYY-MM-DD.",
                CommandOptionType.SingleValue,
                o => o.Accepts().RegularExpression(@"\d{4}-\d{2}-\d{2}", "The date must match the pattern YYYY-MM-DD."));
            var maxTransactionDateOption = app.Option(
                "--max-transaction-date <DATE>",
                "Optional. The maximum, inclusive transaction date. Use format YYYY-MM-DD.",
                CommandOptionType.SingleValue,
                o => o.Accepts().RegularExpression(@"\d{4}-\d{2}-\d{2}", "The date must match the pattern YYYY-MM-DD."));
            var indentedOption = app.Option(
                "--indented",
                "Indent the JSON so that it is more human readable.",
                CommandOptionType.NoValue);
            var databasePathOption = app.DatabasePathOption();
            app.CustomHelpOption();

            app.OnExecute(async () =>
            {
                var settingsFactory = serviceProvider.GetRequiredService<KrogerScrapeSettingsFactory>();
                settingsFactory.Initialize(new KrogerScrapeSettings
                {
                    Email = emailOption.Value().Trim(),
                    DatabasePath = databasePathOption.GetDatabasePath(),
                });

                var parameters = new JsonCommandParameters(
                    jsonPathOption.Value(),
                    minTransactionDateOption.Value(),
                    maxTransactionDateOption.Value(),
                    indentedOption.HasValue());

                await ExecuteAsync(parameters, serviceProvider, logger, token);
            });
        }

        private static async Task ExecuteAsync(
            JsonCommandParameters parameters,
            IServiceProvider serviceProvider,
            ILogger logger,
            CancellationToken token)
        {
            var entityContextFactory = serviceProvider.GetRequiredService<EntityContextFactory>();
            using (var entityContext = entityContextFactory.Create())
            {
                await entityContext.MigrateAsync(token);
            }

            var command = serviceProvider.GetRequiredService<JsonCommandLogic>();
            
            var json = await command.ExecuteAsync(parameters, token);

            Console.WriteLine(json);
        }

        private class JsonPathValidator : IOptionValidator
        {
            public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
            {
                if (!option.HasValue())
                {
                    return ValidationResult.Success;
                }

                try
                {
                    new JObject().SelectToken(option.Value());
                    return ValidationResult.Success;
                }
                catch (JsonException ex)
                {
                    return new ValidationResult("The JSONPath is invalid. " + ex.Message);
                }
            }
        }
    }
}
