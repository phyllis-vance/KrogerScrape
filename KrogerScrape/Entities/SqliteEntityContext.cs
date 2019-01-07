using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KrogerScrape.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KrogerScrape.Entities
{
    public class SqliteEntityContext : DbContext, IEntityContext
    {
        private readonly ILogger<SqliteEntityContext> _logger;

        public SqliteEntityContext(
            DbContextOptions<SqliteEntityContext> options,
            ILogger<SqliteEntityContext> logger) : base(options)
        {
            _logger = logger;
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<OperationEntity> Operations { get; set; }
        public DbSet<CommandEntity> Commands { get; set; }
        public DbSet<SignInEntity> SignIns { get; set; }
        public DbSet<GetReceiptSummariesEntity> GetReceiptSummaries { get; set; }
        public DbSet<GetReceiptEntity> GetReceipts { get; set; }
        public DbSet<ReceiptIdEntity> ReceiptIds { get; set; }
        public DbSet<ResponseEntity> Responses { get; set; }

        public async Task MigrateAsync(CancellationToken token)
        {
            var appliedMigrations = await Database.GetAppliedMigrationsAsync(token);
            var pendingMigrations = await Database.GetPendingMigrationsAsync(token);
            if (!appliedMigrations.Any())
            {
                _logger.LogInformation("The database needs to be initialized.");
            }
            else if (pendingMigrations.Any())
            {
                _logger.LogInformation("The database needs is not up to date and will be updated.");
            }
            else
            {
                return;
            }

            await Database.MigrateAsync(token);
            _logger.LogInformation("The database is now ready.");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<UserEntity>()
                .HasIndex(x => new { x.Email })
                .IsUnique();

            modelBuilder
                .Entity<ReceiptIdEntity>()
                .HasIndex(x => new { x.UserEntityId, x.DivisionNumber, x.StoreNumber, x.TransactionDate, x.TerminalNumber, x.TransactionId })
                .IsUnique();

            modelBuilder
                .Entity<OperationEntity>()
                .HasDiscriminator(x => x.Type)
                .HasValue<OperationEntity>(OperationType.Uncategorized)
                .HasValue<CommandEntity>(OperationType.Command)
                .HasValue<SignInEntity>(OperationType.SignIn)
                .HasValue<GetReceiptSummariesEntity>(OperationType.GetReceiptSummaries)
                .HasValue<GetReceiptEntity>(OperationType.GetReceipt);

            modelBuilder
                .Entity<OperationEntity>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .IsRequired(false);
        }
    }
}
