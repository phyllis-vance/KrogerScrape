using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace KrogerScrape.Entities
{
    public interface IEntityContext : IDisposable
    {
        DbSet<CommandEntity> Commands { get; }
        DbSet<GetReceiptEntity> GetReceipts { get; }
        DbSet<GetReceiptSummariesEntity> GetReceiptSummaries { get; }
        DbSet<OperationEntity> Operations { get; }
        DbSet<ReceiptIdEntity> ReceiptIds { get; }
        DbSet<ResponseEntity> Responses { get; }
        DbSet<SignInEntity> SignIns { get; }
        DbSet<UserEntity> Users { get; }
        Task<int> SaveChangesAsync();
        Task MigrateAsync();
    }
}