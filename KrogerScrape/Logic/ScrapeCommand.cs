using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KrogerScrape.Client;
using KrogerScrape.Entities;
using KrogerScrape.Settings;
using Microsoft.Extensions.Logging;

namespace KrogerScrape.Logic
{
    public class ScrapeCommand
    {
        private readonly EntityRepository _entityRepository;
        private readonly KrogerClientFactory _krogerClientFactory;
        private readonly IKrogerScrapeSettings _settings;
        private readonly ILogger<ScrapeCommand> _logger;

        public ScrapeCommand(
            EntityRepository entityRepository,
            KrogerClientFactory krogerClientFactory,
            IKrogerScrapeSettings settings,
            ILogger<ScrapeCommand> logger)
        {
            _entityRepository = entityRepository;
            _krogerClientFactory = krogerClientFactory;
            _settings = settings;
            _logger = logger;
        }

        public async Task<bool> ExecuteAsync(
            CancellationToken token)
        {
            using (var krogerClient = _krogerClientFactory.Create())
            {
                // Initialize the database and the browser.
                krogerClient.KillOrphanBrowsers();

                var userEntity = await _entityRepository.GetOrAddUserAsync(_settings.Email, token);
                var commandEntity = await _entityRepository.StartCommandAsync(userEntity.Id, token);

                SignInEntity signIn = null;
                GetReceiptSummariesEntity getReceiptSummaries = null;
                var getReceipts = new Dictionary<ReceiptId, GetReceiptEntity>();
                krogerClient.AddResponseRecordListener(
                    async response =>
                    {
                        OperationEntity operationEntity;
                        switch (response.OperationType)
                        {
                            case OperationType.SignIn:
                                operationEntity = signIn;
                                break;
                            case OperationType.GetReceiptSummaries:
                                operationEntity = getReceiptSummaries;
                                break;
                            case OperationType.GetReceipt:
                                operationEntity = getReceipts[(ReceiptId)response.OperationParameters];
                                break;
                            default:
                                return;
                        }

                        await _entityRepository.RecordResponseAsync(operationEntity.Id, response, CancellationToken.None);
                    });

                await krogerClient.InitializeAsync();

                // Sign in.
                token.ThrowIfCancellationRequested();
                _logger.LogInformation("Logging in with email address {Email}.", _settings.Email);
                signIn = await _entityRepository.StartSignInAsync(commandEntity.Id, token);
                var signInResponse = await krogerClient.SignInAsync(token);
                await _entityRepository.CompleteOperationAsync(signIn, token);

                if (signInResponse == null)
                {
                    _logger.LogError("No sign in data was found.");
                    return false;
                }

                if (signInResponse.Response.AuthenticationState?.Authenticated != true)
                {
                    _logger.LogError("The sign in was not successful. Verify your email and password.");
                    return false;
                }

                // Fetch the receipt summaries.
                token.ThrowIfCancellationRequested();
                _logger.LogInformation("Fetching the receipt summaries.");
                getReceiptSummaries = await _entityRepository.StartGetReceiptSummariesAsync(commandEntity.Id, token);
                var receiptSummaries = await krogerClient.GetReceiptSummariesAsync(token);
                await _entityRepository.CompleteOperationAsync(getReceiptSummaries, token);

                if (receiptSummaries == null)
                {
                    _logger.LogError("No receipt summary data was found.");
                    return false;
                }

                var ascendingChronological = receiptSummaries
                    .Response
                    .OrderBy(x => DateTimeOffset.ParseExact(x.ReceiptId.TransactionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                    .ToList();
                _logger.LogInformation(
                    "Found {Count} receipts. Processing them from oldest to newest.",
                    ascendingChronological.Count);

                // Fetch each receipt.
                var receiptNumber = 0;
                var alreadyCompleteCount = 0;
                foreach (var receiptSummary in ascendingChronological)
                {
                    token.ThrowIfCancellationRequested();

                    receiptNumber++;

                    var receiptId = receiptSummary.ReceiptId;
                    var receiptDate = receiptId.TransactionDate;

                    if (getReceipts.ContainsKey(receiptId))
                    {
                        _logger.LogWarning(
                            "A receipt on {ReceiptTransactionDate} appeared more than one in the receipt summaries. It will only be fetched once.",
                            receiptDate);
                        alreadyCompleteCount++;
                        continue;
                    }

                    var receiptEntity = await _entityRepository.GetOrAddReceiptAsync(userEntity.Id, receiptId, token);
                    receiptEntity = await _entityRepository.TrySetLatestReceiptResponseAsync(receiptEntity.Id, token);
                    if (receiptEntity.ReceiptResponseEntity != null)
                    {
                        _logger.LogDebug(
                            "A receipt on {ReceiptTransactionDate} has already been fetched in the past and will therefore be skipped.",
                            receiptDate);
                        alreadyCompleteCount++;
                        continue;
                    }

                    var receiptUrl = krogerClient.GetReceiptUrl(receiptId);
                    _logger.LogInformation(
                        Environment.NewLine +
                        $"Fetching receipt {{Number}} of {{Total}}.{Environment.NewLine}" +
                        $"  URL:  {{ReceiptUrl}}{Environment.NewLine}" +
                        $"  Date: {{ReceiptTransactionDate}}",
                        receiptNumber,
                        ascendingChronological.Count,
                        receiptUrl,
                        receiptId.TransactionDate);

                    var getReceipt = await _entityRepository.StartGetReceiptAsync(commandEntity.Id, receiptEntity.Id, token);
                    getReceipts[receiptId] = getReceipt;
                    var receipt = await krogerClient.GetReceiptAsync(receiptId, token);
                    await _entityRepository.CompleteOperationAsync(getReceipt, token);

                    if (receipt == null)
                    {
                        _logger.LogError("No receipt data was found for {{ReceiptUrl}}.", receiptUrl);
                        return false;
                    }

                    await _entityRepository.TrySetLatestReceiptResponseAsync(
                        receiptEntity.Id,
                        receipt.RequestId,
                        token);

                    _logger.LogInformation(
                        "Done. The receipt had {ItemCount} items, totaling {Amount}.",
                        receipt.Response.TotalLineItems,
                        receipt.Response.Total.Value.ToString("C", CultureInfo.CreateSpecificCulture("en-US")));
                }

                if (alreadyCompleteCount > 0)
                {
                    _logger.LogInformation("{Count} receipts have already been fetched and were therefore skipped.", alreadyCompleteCount);
                }

                await _entityRepository.CompleteOperationAsync(commandEntity, token);

                return true;
            }
        }
    }
}
