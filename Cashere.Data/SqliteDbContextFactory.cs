using Microsoft.EntityFrameworkCore;

namespace Cashere.Data;

// The embedded Kestrel server (CashereServerHost) registers its own
// CashereDbContext via its own DI container, entirely separate from the UI.
// The UI layer needs its own way to get short-lived contexts per operation
// (the recommended EF Core pattern for desktop apps), hence this small
// hand-rolled factory rather than sharing a single long-lived context.
public class SqliteDbContextFactory : IDbContextFactory<CashereDbContext>
{
    private readonly string _connectionString;

    public SqliteDbContextFactory(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public CashereDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CashereDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new CashereDbContext(options);
    }
}
