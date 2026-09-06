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

    public async Task<ShopSettings?> GetSettingsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.ShopSettings.AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task UpdateSettingsAsync(ShopSettings settings)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var existing = await db.ShopSettings.FirstOrDefaultAsync();
        if (existing is null)
        {
            db.ShopSettings.Add(new ShopSettings
            {
                ShopName = settings.ShopName,
                Address = settings.Address,
                Phone = settings.Phone,
                Currency = settings.Currency,
                TaxRatePercent = settings.TaxRatePercent,
                ReceiptFooterText = settings.ReceiptFooterText
            });
        }
        else
        {
            existing.ShopName = settings.ShopName;
            existing.Address = settings.Address;
            existing.Phone = settings.Phone;
            existing.Currency = settings.Currency;
            existing.TaxRatePercent = settings.TaxRatePercent;
            existing.ReceiptFooterText = settings.ReceiptFooterText;
        }

        await db.SaveChangesAsync();
    }
}