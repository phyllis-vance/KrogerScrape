using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KrogerScrape.Client;
using KrogerScrape.Entities;
using KrogerScrape.Support;
using Microsoft.EntityFrameworkCore;

namespace KrogerScrape.Logic
{
    public class EntityRepository
    {
        private readonly EntityContextFactory _entityContextFactory;

        public EntityRepository(EntityContextFactory entityContextFactory)
        {
            _entityContextFactory = entityContextFactory;
        }

        public async Task<UserEntity> GetOrAddUserAsync(
            string email,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entity = await entityContext
                   .Users
                   .Where(x => x.Email == email)
                   .FirstOrDefaultAsync(token);

                if (entity == null)
                {
                    entity = new UserEntity { Email = email };

                    await entityContext.Users.AddAsync(entity, token);
                    await entityContext.SaveChangesAsync(token);
                }

                return entity;
            }
        }

        public async Task<ReceiptIdEntity> GetOrAddReceiptIdAsync(
            long userEntityId,
            ReceiptId receiptId,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var entity = await entityContext
                    .ReceiptIds
                    .Include(x => x.GetReceiptOperationEntities)
                    .ThenInclude(x => x.ResponseEntities)
                    .Where(x => x.DivisionNumber == receiptId.DivisionNumber
                             && x.StoreNumber == receiptId.StoreNumber
                             && x.TransactionDate == receiptId.TransactionDate
                             && x.TerminalNumber == receiptId.TerminalNumber
                             && x.TransactionId == receiptId.TransactionId)
                    .FirstOrDefaultAsync(token);

                if (entity == null)
                {
                    entity = new ReceiptIdEntity
                    {
                        UserEntityId = userEntityId,
                        DivisionNumber = receiptId.DivisionNumber,
                        StoreNumber = receiptId.StoreNumber,
                        TransactionDate = receiptId.TransactionDate,
                        TerminalNumber = receiptId.TerminalNumber,
                        TransactionId = receiptId.TransactionId,
                        GetReceiptOperationEntities = new List<GetReceiptEntity>(),
                    };

                    await entityContext.ReceiptIds.AddAsync(entity, token);
                    await entityContext.SaveChangesAsync(token);
                }

                return entity;
            }
        }

        public async Task RecordResponseAsync(
            long operationEntityId,
            Response response,
            CancellationToken token)
        {
            using (var entityContext = _entityContextFactory.Create())
            {
                var uncompressedBytes = Encoding.UTF8.GetBytes(response.Body);
                var compressedBytes = CompressionUtility.Compress(uncompressedBytes);
                var isCompressed = compressedBytes.Length < uncompressedBytes.Length;
                var bytes = isCompressed ? compressedBytes : uncompressedBytes;

                var entity = new ResponseEntity
                {
                    OperationEntityId = operationEntityId,
                    RequestType = response.RequestType,
                    CompletedTimestamp = response.CompletedTimestamp,
                    Method = response.Method.Method,
                    Url = response.Url,
                    CompressionType = isCompressed ? CompressionType.Gzip : CompressionType.None,
                    Body = bytes,
                };

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
