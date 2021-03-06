﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KrogerScrape.Client;
using KrogerScrape.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KrogerScrape.Logic
{
    public class EntityRepository
    {
        private readonly EntityContextFactory _entityContextFactory;
        private readonly Deserializer _deserializer;

        public EntityRepository(
            EntityContextFactory entityContextFactory,
            Deserializer deserializer)
        {
            _entityContextFactory = entityContextFactory;
            _deserializer = deserializer;
        }

        public async Task<UserEntity> GetOrAddUserAsync(
            string email,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entities = await entityContext
                   .Users
                   .Where(x => x.Email.ToLower() == email.ToLower())
                   .ToListAsync(token);

                if (entities.Count > 1)
                {
                    throw new InvalidOperationException("There duplicate users with this email address.");
                }

                var entity = entities.FirstOrDefault();
                if (entity == null)
                {
                    entity = new UserEntity { Email = email };

                    await entityContext.Users.AddAsync(entity, token);
                    await entityContext.SaveChangesAsync(token);
                }

                return entity;
            }
        }

        public async Task<List<ReceiptEntity>> GetAllReceiptsAsync(string email, CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                return await entityContext
                    .Receipts
                    .Where(x => x.UserEntity.Email == email)
                    .ToListAsync(token);
            }
        }

        public async Task<List<ResponseEntity>> GetResponsesAsync(
            string email,
            string minTransactionDate,
            string maxTranscationDate,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                IQueryable<ReceiptEntity> query = entityContext.Receipts;

                if (email != null)
                {
                    query = query.Where(x => x.UserEntity.Email == email);
                }

                if (minTransactionDate != null)
                {
                    query = query.Where(x => x.TransactionDate.CompareTo(minTransactionDate) >= 0);
                }

                if (maxTranscationDate != null)
                {
                    query = query.Where(x => x.TransactionDate.CompareTo(maxTranscationDate) <= 0);
                }

                return await query
                    .Select(x => x.ReceiptResponseEntity)
                    .ToListAsync();
            }
        }

        public async Task<ReceiptEntity> GetOrAddReceiptAsync(
            long userEntityId,
            ReceiptId receiptId,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entity = await entityContext
                    .Receipts
                    .Include(x => x.ReceiptResponseEntity)
                    .Where(x => x.DivisionNumber == receiptId.DivisionNumber
                             && x.StoreNumber == receiptId.StoreNumber
                             && x.TransactionDate == receiptId.TransactionDate
                             && x.TerminalNumber == receiptId.TerminalNumber
                             && x.TransactionId == receiptId.TransactionId)
                    .FirstOrDefaultAsync(token);

                if (entity == null)
                {
                    entity = new ReceiptEntity
                    {
                        UserEntityId = userEntityId,
                        DivisionNumber = receiptId.DivisionNumber,
                        StoreNumber = receiptId.StoreNumber,
                        TransactionDate = receiptId.TransactionDate,
                        TerminalNumber = receiptId.TerminalNumber,
                        TransactionId = receiptId.TransactionId,
                        GetReceiptOperationEntities = new List<GetReceiptEntity>(),
                    };

                    await entityContext.Receipts.AddAsync(entity, token);
                    await entityContext.SaveChangesAsync(token);
                }

                return entity;
            }
        }

        public async Task<ReceiptEntity> TrySetLatestReceiptResponseAsync(
            long receiptId,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var receipt = await entityContext
                    .Receipts
                    .Include(x => x.ReceiptResponseEntity)
                    .Include(x => x.GetReceiptOperationEntities)
                    .ThenInclude(x => x.ResponseEntities)
                    .Where(x => x.Id == receiptId)
                    .SingleAsync();

                var responses = receipt
                    .GetReceiptOperationEntities
                    .SelectMany(x => x.ResponseEntities)
                    .OrderByDescending(x => x.CompletedTimestamp)
                    .ToList();

                if (!responses.Any())
                {
                    if (receipt.ReceiptResponseEntity != null)
                    {
                        receipt.ReceiptResponseEntity = null;
                        await entityContext.SaveChangesAsync(token);
                    }

                    return receipt;
                }

                foreach (var response in responses)
                {
                    try
                    {
                        var json = response.Decompress();
                        var deserializedReceipt = _deserializer.Receipt(json);

                        var rid = deserializedReceipt.ReceiptId;
                        if (rid.DivisionNumber != receipt.DivisionNumber
                            || rid.StoreNumber != receipt.StoreNumber
                            || rid.TerminalNumber != receipt.TerminalNumber
                            || rid.TransactionDate != receipt.TransactionDate
                            || rid.TransactionId != receipt.TransactionId)
                        {
                            continue;
                        }
                    }
                    catch (JsonException)
                    {
                        // This response can't be treated like a response. Let's move on.
                        continue;
                    }

                    receipt.ReceiptResponseEntity = response;
                    await entityContext.SaveChangesAsync(token);
                    break;
                }

                return receipt;
            }
        }

        public async Task TrySetLatestReceiptResponseAsync(
            long receiptId,
            string requestId,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var receipt = await entityContext
                    .Receipts
                    .Include(x => x.ReceiptResponseEntity)
                    .Where(x => x.Id == receiptId)
                    .SingleAsync();

                var response = await entityContext
                    .Responses
                    .Where(x => x.RequestId == requestId)
                    .Where(x => x.RequestType == RequestType.ReceiptDetail)
                    .SingleAsync();

                if (receipt.ReceiptResponseEntity == null
                    || receipt.ReceiptResponseEntity.CompletedTimestamp < response.CompletedTimestamp)
                {
                    receipt.ReceiptResponseEntity = response;
                    await entityContext.SaveChangesAsync(token);
                }
            }
        }

        public async Task RecordResponseAsync(
            long operationEntityId,
            Response response,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entity = new ResponseEntity
                {
                    OperationEntityId = operationEntityId,
                    RequestId = response.RequestId,
                    RequestType = response.RequestType,
                    CompletedTimestamp = response.CompletedTimestamp,
                    Method = response.Method.Method,
                    Url = response.Url,
                };

                entity.Compress(response.Body);

                await entityContext.Responses.AddAsync(entity, token);
                await entityContext.SaveChangesAsync(token);
            }
        }

        public async Task<CommandEntity> StartCommandAsync(
            long userEntityId,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entity = new CommandEntity
                {
                    UserEntityId = userEntityId,
                    StartedTimestamp = DateTimeOffset.UtcNow,
                };

                await entityContext.Commands.AddAsync(entity, token);
                await entityContext.SaveChangesAsync(token);

                return entity;
            }
        }

        public async Task<SignInEntity> StartSignInAsync(
            long commandEntityId,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entity = new SignInEntity
                {
                    ParentId = commandEntityId,
                    StartedTimestamp = DateTimeOffset.UtcNow,
                };

                await entityContext.SignIns.AddAsync(entity, token);
                await entityContext.SaveChangesAsync(token);

                return entity;
            }
        }

        public async Task<GetReceiptSummariesEntity> StartGetReceiptSummariesAsync(
            long commandEntityId,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entity = new GetReceiptSummariesEntity
                {
                    ParentId = commandEntityId,
                    StartedTimestamp = DateTimeOffset.UtcNow,
                };

                await entityContext.Operations.AddAsync(entity, token);
                await entityContext.SaveChangesAsync(token);

                return entity;
            }
        }

        public async Task<GetReceiptEntity> StartGetReceiptAsync(
            long commandEntityId,
            long receiptEntityId,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entity = new GetReceiptEntity
                {
                    ParentId = commandEntityId,
                    ReceiptEntityId = receiptEntityId,
                    StartedTimestamp = DateTimeOffset.UtcNow,
                };

                await entityContext.Operations.AddAsync(entity);
                await entityContext.SaveChangesAsync(token);

                return entity;
            }
        }

        public async Task CompleteOperationAsync(
            OperationEntity operationEntity,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                entityContext.Operations.Attach(operationEntity);
                operationEntity.CompletedTimestamp = DateTimeOffset.UtcNow;
                await entityContext.SaveChangesAsync(token);
            }
        }
    }
}
