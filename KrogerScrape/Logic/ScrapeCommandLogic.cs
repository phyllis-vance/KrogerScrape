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
    public class ScrapeCommandLogic
    {
        private readonly EntityRepository _entityRepository;
        private readonly KrogerClientFactory _krogerClientFactory;
        private readonly IKrogerScrapeSettings _settings;
        private readonly ILogger<ScrapeCommandLogic> _logger;

        public ScrapeCommandLogic(
            EntityRepository entityRepository,
            KrogerClientFactory krogerClientFactory,
            IKrogerScrapeSettings settings,
            ILogger<ScrapeCommandLogic> logger)
        {
            _entityRepository = entityRepository;
            _krogerClientFactory = krogerClientFactory;
            _settings = settings;
            _logger = logger;
        }

        public async Task<bool> ExecuteAsync(
            CancellationToken token)
        {
            using (var client = _krogerClientFactory.Create())
            {
                var ctx = await InitializeContextAsync(client, token);

                await SetLatestReceiptResponsesAsync(ctx);

                await InitializeClientAsync(ctx);

                if (!await SignInAsync(ctx))
                {
                    return false;
                }

                var receiptSummaries = await GetReceiptSummariesAsync(ctx);
                if (receiptSummaries == null)
                {
                    return false;
                }

                var receiptNumber = 0;
                foreach (var receiptSummary in receiptSummaries)
                {
                    receiptNumber++;
                    if (!await GetReceiptAsync(ctx, receiptSummary, receiptNumber, receiptSummaries.Count))
                    {
                        return false;
                    }
                }

                await CompleteAsync(ctx);

                return true;
            }
        }

        private async Task<Context> InitializeContextAsync(KrogerClient krogerClient, CancellationToken token)
        {
            var userEntity = await _entityRepository.GetOrAddUserAsync(_settings.Email, token);
            var commandEntity = await _entityRepository.StartCommandAsync(userEntity.Id, token);
            return new Context(krogerClient, userEntity, commandEntity, token);
        }

        private async Task SetLatestReceiptResponsesAsync(Context ctx)
        {
            // Make sure all existing receipts have the latest response.
            _logger.LogInformation("Making sure all existing receipts have response data.");
            var allReceipts = await _entityRepository.GetAllReceiptsAsync(ctx.User.Email, ctx.Token);
            foreach (var receipt in allReceipts)
            {
                var updated = await _entityRepository.TrySetLatestReceiptResponseAsync(receipt.Id, ctx.Token);
                if (updated.ReceiptResponseEntity == null)
                {
                    _logger.LogWarning("Receipt at {ReceiptUrl} has no receipt data.", receipt.GetReceiptId().GetUrl());
                }
            }
        }

        private async Task InitializeClientAsync(Context ctx)
        {
            ctx.Client.KillOrphanBrowsers();

            ctx.Client.AddResponseRecordListener(
                async response =>
                {
                    OperationEntity operationEntity;
                    switch (response.OperationType)
                    {
                        case OperationType.SignIn:
                            operationEntity = ctx.SignIn;
                            break;
                        case OperationType.GetReceiptSummaries:
                            operationEntity = ctx.GetReceiptSummaries;
                            break;
                        case OperationType.GetReceipt:
                            operationEntity = ctx.GetReceipts[(ReceiptId)response.OperationParameters];
                            break;
                        default:
                            return;
                    }

                    await _entityRepository.RecordResponseAsync(operationEntity.Id, response, CancellationToken.None);
                });

            await ctx.Client.InitializeAsync();
        }

        private async Task<bool> SignInAsync(Context ctx)
        {
            ctx.Token.ThrowIfCancellationRequested();
            _logger.LogInformation("Logging in with email address {Email}.", _settings.Email);
            ctx.SignIn = await _entityRepository.StartSignInAsync(ctx.Command.Id, ctx.Token);
            var signInResponse = await ctx.Client.SignInAsync(ctx.Token);
            await _entityRepository.CompleteOperationAsync(ctx.SignIn, ctx.Token);

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

            return true;
        }

        private async Task<List<Receipt>> GetReceiptSummariesAsync(Context ctx)
        {
            ctx.Token.ThrowIfCancellationRequested();
            _logger.LogInformation("Fetching the receipt summaries.");
            ctx.GetReceiptSummaries = await _entityRepository.StartGetReceiptSummariesAsync(ctx.Command.Id, ctx.Token);
            var receiptSummaries = await ctx.Client.GetReceiptSummariesAsync(ctx.Token);
            await _entityRepository.CompleteOperationAsync(ctx.GetReceiptSummaries, ctx.Token);

            if (receiptSummaries == null)
            {
                _logger.LogError("No receipt summary data was found.");
                return null;
            }

            var ascendingChronological = receiptSummaries
                .Response
                .OrderBy(x => DateTimeOffset.ParseExact(x.ReceiptId.TransactionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                .ToList();
            _logger.LogInformation(
                "Found {Count} receipts. Processing them from oldest to newest.",
                ascendingChronological.Count);

            return ascendingChronological;
        }

        private async Task<bool> GetReceiptAsync(Context ctx, Receipt receiptSummary, int receiptNumber, int receiptTotal)
        {
            ctx.Token.ThrowIfCancellationRequested();

            var receiptId = receiptSummary.ReceiptId;
            var receiptDate = receiptId.TransactionDate;

            if (ctx.GetReceipts.ContainsKey(receiptId))
            {
                _logger.LogWarning(
                    "A receipt on {ReceiptTransactionDate} appeared more than one in the receipt summaries. It will only be fetched once.",
                    receiptDate);
                ctx.AlreadyComplete++;
                return true;
            }

            var receiptEntity = await _entityRepository.GetOrAddReceiptAsync(ctx.User.Id, receiptId, ctx.Token);
            if (!_settings.RefetchReceipts
                && receiptEntity.ReceiptResponseEntity != null)
            {
                _logger.LogDebug(
                    "A receipt on {ReceiptTransactionDate} has already been fetched in the past and will therefore be skipped.",
                    receiptDate);
                ctx.AlreadyComplete++;
                return true;
            }

            var receiptUrl = receiptId.GetUrl();
            _logger.LogInformation(
                Environment.NewLine +
                $"Fetching receipt {{Number}} of {{Total}}.{Environment.NewLine}" +
                $"  URL:  {{ReceiptUrl}}{Environment.NewLine}" +
                $"  Date: {{ReceiptTransactionDate}}",
                receiptNumber,
                receiptTotal,
                receiptUrl,
                receiptId.TransactionDate);

            var getReceipt = await _entityRepository.StartGetReceiptAsync(ctx.Command.Id, receiptEntity.Id, ctx.Token);
            ctx.GetReceipts[receiptId] = getReceipt;
            var receipt = await ctx.Client.GetReceiptAsync(receiptId, ctx.Token);
            await _entityRepository.CompleteOperationAsync(getReceipt, ctx.Token);

            if (receipt == null)
            {
                _logger.LogError("No receipt data was found for {ReceiptUrl}.", receiptUrl);
                return false;
            }

            if (receipt.Response.ReceiptId != receiptId)
            {
                _logger.LogError("Mismatched receipt data was returned for {ReceiptUrl}.", receiptUrl);
                return false;
            }

            await _entityRepository.TrySetLatestReceiptResponseAsync(
                receiptEntity.Id,
                receipt.RequestId,
                ctx.Token);

            _logger.LogInformation(
                "Done. The receipt had {ItemCount} items, totaling {Amount}.",
                receipt.Response.TotalLineItems,
                receipt.Response.Total.Value.ToString("C", CultureInfo.CreateSpecificCulture("en-US")));
            return true;
        }

        private async Task CompleteAsync(Context ctx)
        {
            if (ctx.AlreadyComplete > 0)
            {
                _logger.LogInformation("{Count} receipts have already been fetched and were therefore skipped.", ctx.AlreadyComplete);
            }

            await _entityRepository.CompleteOperationAsync(ctx.Command, ctx.Token);
        }

        private class Context
        {
            public Context(
                KrogerClient client,
                UserEntity user,
                CommandEntity command,
                CancellationToken token)
            {
                Client = client;
                User = user;
                Command = command;
                Token = token;
                GetReceipts = new Dictionary<ReceiptId, GetReceiptEntity>();
                AlreadyComplete = 0;
            }

            public KrogerClient Client { get; }
            public UserEntity User { get; }
            public CommandEntity Command { get; }
            public CancellationToken Token { get; }
            public SignInEntity SignIn { get; set; }
            public GetReceiptSummariesEntity GetReceiptSummaries { get; set; }
            public Dictionary<ReceiptId, GetReceiptEntity> GetReceipts { get; }
            public int AlreadyComplete { get; set; }
        }
    }
}
