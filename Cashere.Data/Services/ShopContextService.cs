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
        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();
        return settings?.TaxRatePercent ?? 0;
    }

    public async Task<string> GetShopNameAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();
        return settings?.ShopName ?? "Cashere";
    }

    public async Task<ReceiptAdmin?> GetSettingsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();
    }

    // Attaches and marks the whole entity Modified (or Adds it, if no row
    // exists yet) instead of copying fields one by one onto a separately
    // fetched, separately tracked instance. The old field-by-field version
    // silently dropped every property added to ReceiptAdmin after it was
    // written - Email/TaxId/Timezone from Business Info, and TrackInventory/
    // OutOfStockBehavior/DefaultLowStockThreshold/AutoGenerateSku/
    // AutoGenerateBarcode from Inventory all went unsaved. This version
    // can't drift out of sync with the model again.
    public async Task UpdateSettingsAsync(ReceiptAdmin settings)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        if (settings.Id == 0)
        {
            db.ReceiptAdmin.Add(settings);
        }
        else
        {
            db.ReceiptAdmin.Update(settings);
        }

        await db.SaveChangesAsync();
    }

    public async Task<PaymentSettings> GetPaymentSettingsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();

        return new PaymentSettings(
            settings?.CashEnabled ?? true,
            settings?.QrisEnabled ?? true,
            settings?.EdcEnabled ?? true,
            settings?.QrisAccountInfo,
            settings?.EdcAccountInfo,
            settings?.RequireConfirmationForNonCash ?? false);
    }
}