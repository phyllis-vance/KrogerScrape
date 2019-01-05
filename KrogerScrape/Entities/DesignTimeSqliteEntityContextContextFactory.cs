using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrogerScrape.Entities
{
    public class DesignTimeSqliteEntityContextContextFactory : IDesignTimeDbContextFactory<SqliteEntityContext>
    {
        public SqliteEntityContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqliteEntityContext>();

            optionsBuilder
                .UseSqlite("Data Source=KrogerScrape.db");

            return new SqliteEntityContext(optionsBuilder.Options, new NullLogger<SqliteEntityContext>());
        }
    }
}
