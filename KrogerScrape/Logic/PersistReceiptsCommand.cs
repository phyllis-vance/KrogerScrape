using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KrogerScrape.Client;
using KrogerScrape.Entities;
using Microsoft.Extensions.Logging;

namespace KrogerScrape.Logic
{
    public class PersistReceiptsCommand
    {
        private readonly EntityRepository _entityRepository;
        private readonly KrogerClientFactory _krogerClientFactory;
        private readonly ILogger<PersistReceiptsCommand> _logger;

        public PersistReceiptsCommand(
            EntityRepository entityRepository,
            KrogerClientFactory krogerClientFactory,
            ILogger<PersistReceiptsCommand> logger)
        {
            _entityRepository = entityRepository;
            _krogerClientFactory = krogerClientFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(string email, string password, bool fetchAgain)
        {
            using (var krogerClient = _krogerClientFactory.Create())
            {
                var userEntity = await _entityRepository.GetOrAddUserAsync(email);
                var commandEntity = await _entityRepository.StartCommandAsync(userEntity.Id);

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

                        await _entityRepository.RecordResponseAsync(operationEntity.Id, response);
                    });

                try
                {
                    krogerClient.KillOrphanBrowsers();
                    await krogerClient.InitializeAsync();

                    _logger.LogInformation("Logging in with email address {Email}.", email);
                    signIn = await _entityRepository.StartSignInAsync(commandEntity.Id);
                    var signInResponse = await krogerClient.SignInAsync(email, password);
                    await _entityRepository.CompleteOperationAsync(signIn);
                    if (signInResponse?.AuthenticationState?.Authenticated == false)
                    {
                        throw new InvalidOperationException("The sign in operation did not succeed.");
                    }

                    _logger.LogInformation("Fetching the receipt summaries.");
                    getReceiptSummaries = await _entityRepository.StartGetReceiptSummariesAsync(commandEntity.Id);
                    var receiptSummaries = await krogerClient.GetReceiptSummariesAsync();
                    await _entityRepository.CompleteOperationAsync(getReceiptSummaries);

                    _logger.LogInformation("Found {Count} receipts. Processing them from oldest to newest.", receiptSummaries.Count);
                    var ascendingChronological = receiptSummaries
                        .OrderBy(x => DateTimeOffset.ParseExact(x.ReceiptId.TransactionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                        .ToList();

                    var receiptNumber = 0;
                    foreach (var receiptSummary in ascendingChronological)
                    {
                        receiptNumber++;

                        var receiptId = receiptSummary.ReceiptId;
                        var receiptDate = receiptId.TransactionDate;

                        if (getReceipts.ContainsKey(receiptId))
                        {
                            _logger.LogWarning(
                                "A receipt on {ReceiptTransactionDate} appeared more than one in the receipt summaries. It will only be fetched once.",
                                receiptDate);
                            continue;
                        }

                        var receiptIdEntity = await _entityRepository.GetOrAddReceiptIdAsync(userEntity.Id, receiptId);
                        if (!fetchAgain && receiptIdEntity.GetReceiptOperationEntities.Any())
                        {
                            _logger.LogInformation(
                                "A receipt on {ReceiptTransactionDate} has already been fetched in the past and will therefore be skipped.",
                                receiptDate);
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

                        var getReceipt = await _entityRepository.StartGetReceiptAsync(commandEntity.Id, receiptIdEntity.Id);
                        getReceipts[receiptId] = getReceipt;
                        var receipt = await krogerClient.GetReceiptAsync(receiptId);
                        await _entityRepository.CompleteOperationAsync(getReceipt);

                        _logger.LogInformation(
                            "Done. The receipt had {ItemCount} items, totaling {Amount}.",
                            receipt.TotalLineItems,
                            receipt.Total.Value.ToString("C", CultureInfo.CreateSpecificCulture("en-US")));
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        await krogerClient.CompleteAsync();
                    }
                    catch
                    {
                        // This is just a best effort.
                    }

                    throw;
                }

                await krogerClient.CompleteAsync();
                await _entityRepository.CompleteOperationAsync(commandEntity);
            }
        }
    }
}
