using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cashere.Data;

// Lets "dotnet ef" tooling create/apply migrations from the command line
// without needing to launch the Avalonia app itself.
public class CashereDbContextFactory : IDesignTimeDbContextFactory<CashereDbContext>
{
    public CashereDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CashereDbContext>();
        var dbPath = CashereDbContext.GetDefaultDbPath();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new CashereDbContext(optionsBuilder.Options);
    }
}
