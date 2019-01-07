using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KrogerScrape.Entities
{
    public class EntityContextFactory
    {
        private readonly string _databasePath;
        private readonly ILoggerFactory _loggerFactory;

        public EntityContextFactory(string databasePath, ILoggerFactory loggerFactory)
        {
            _databasePath = databasePath;
            _loggerFactory = loggerFactory;
        }

        public IEntityContext Get()
        {
            var builder = new SqliteConnectionStringBuilder();
            builder.DataSource = _databasePath;

            var options = new DbContextOptionsBuilder<SqliteEntityContext>()
                .UseSqlite(builder.ConnectionString)
                .Options;

            var entityContext = new SqliteEntityContext(
                options,
                _loggerFactory.CreateLogger<SqliteEntityContext>());

            return entityContext;
        }
    }
}
