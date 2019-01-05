using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<UserEntity> GetOrAddUserAsync(string email)
        {
            using (var entityContext = await _entityContextFactory.GetAsync())
            {
                var userEntity = await entityContext
                   .Users
                   .Where(x => x.Email == email)
                   .FirstOrDefaultAsync();

                if (userEntity == null)
                {
                    userEntity = new UserEntity { Email = email };
                    entityContext.Users.Add(userEntity);
                    await entityContext.SaveChangesAsync();
                }

                return userEntity;
            }
        }

        public async Task<ReceiptIdEntity> GetOrAddReceiptIdAsync(long userEntityId, ReceiptId receiptId)
        {
            using (var entityContext = await _entityContextFactory.GetAsync())
            {
                var receiptIdEntity = await entityContext
                    .ReceiptIds
                    .Include(x => x.GetReceiptOperationEntities)
                    .Where(x => x.DivisionNumber == receiptId.DivisionNumber
                             && x.StoreNumber == receiptId.StoreNumber
                             && x.TransactionDate == receiptId.TransactionDate
                             && x.TerminalNumber == receiptId.TerminalNumber
                             && x.TransactionId == receiptId.TransactionId)
                    .FirstOrDefaultAsync();

                if (receiptIdEntity == null)
                {
                    receiptIdEntity = new ReceiptIdEntity
                    {
                        UserEntityId = userEntityId,
                        DivisionNumber = receiptId.DivisionNumber,
                        StoreNumber = receiptId.StoreNumber,
                        TransactionDate = receiptId.TransactionDate,
                        TerminalNumber = receiptId.TerminalNumber,
                        TransactionId = receiptId.TransactionId,
                        GetReceiptOperationEntities = new List<GetReceiptEntity>(),
                    };
                    entityContext.ReceiptIds.Add(receiptIdEntity);
                    await entityContext.SaveChangesAsync();
                }

                return receiptIdEntity;
            }
        }

        public async Task RecordResponseAsync(long operationEntityId, Response response)
        {
            using (var entityContext = await _entityContextFactory.GetAsync())
            {
                var uncompressedBytes = Encoding.UTF8.GetBytes(response.Body);
                var compressedBytes = CompressionUtility.Compress(uncompressedBytes);
                var isCompressed = compressedBytes.Length < uncompressedBytes.Length;
                var bytes = isCompressed ? compressedBytes : uncompressedBytes;

                entityContext.Responses.Add(new ResponseEntity
                {
                    OperationEntityId = operationEntityId,
                    RequestType = response.RequestType,
                    CompletedTimestamp = response.CompletedTimestamp,
                    Method = response.Method.Method,
                    Url = response.Url,
                    CompressionType = isCompressed ? CompressionType.Gzip : CompressionType.None,
                    Body = bytes,
                });

                await entityContext.SaveChangesAsync();
            }
        }

        public async Task<CommandEntity> StartCommandAsync(long userEntityId)
        {
            using (var entityContext = await _entityContextFactory.GetAsync())
            {
                var entity = new CommandEntity
                {
                    UserEntityId = userEntityId,
                    StartedTimestamp = DateTimeOffset.UtcNow,
                };

                entityContext.Commands.Add(entity);
                await entityContext.SaveChangesAsync();

                return entity;
            }
        }

        public async Task<SignInEntity> StartSignInAsync(long commandEntityId)
        {
            using (var entityContext = await _entityContextFactory.GetAsync())
            {
                var entity = new SignInEntity
                {
                    ParentId = commandEntityId,
                    StartedTimestamp = DateTimeOffset.UtcNow,
                };

                entityContext.SignIns.Add(entity);
                await entityContext.SaveChangesAsync();

                return entity;
            }
        }

        public async Task<GetReceiptSummariesEntity> StartGetReceiptSummariesAsync(long commandEntityId)
        {
            using (var entityContext = await _entityContextFactory.GetAsync())
            {
                var entity = new GetReceiptSummariesEntity
                {
                    ParentId = commandEntityId,
                    StartedTimestamp = DateTimeOffset.UtcNow,
                };

                entityContext.Operations.Add(entity);
                await entityContext.SaveChangesAsync();

                return entity;
            }
        }

        public async Task<GetReceiptEntity> StartGetReceiptAsync(
            long commandEntityId,
            long receiptEntityId)
        {
            using (var entityContext = await _entityContextFactory.GetAsync())
            {
                var entity = new GetReceiptEntity
                {
                    ParentId = commandEntityId,
                    ReceiptEntityId = receiptEntityId,
                    StartedTimestamp = DateTimeOffset.UtcNow,
                };

                entityContext.Operations.Add(entity);
                await entityContext.SaveChangesAsync();

                return entity;
            }
        }

        public async Task CompleteOperationAsync(OperationEntity operationEntity)
        {
            using (var entityContext = await _entityContextFactory.GetAsync())
            {
                entityContext.Operations.Attach(operationEntity);
                operationEntity.CompletedTimestamp = DateTimeOffset.UtcNow;
                await entityContext.SaveChangesAsync();
            }
        }
    }
}
