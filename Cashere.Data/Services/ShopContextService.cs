using System.Linq;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class ShopContextService : IShopContextService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public ShopContextService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Cashier?> GetDefaultCashierAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Cashiers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<decimal> GetTaxRatePercentAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var settings = await db.ShopSettings.AsNoTracking().FirstOrDefaultAsync();
        return settings?.TaxRatePercent ?? 0;
    }

    public async Task<string> GetShopNameAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var settings = await db.ShopSettings.AsNoTracking().FirstOrDefaultAsync();
        return settings?.ShopName ?? "Cashere";
    }
}
