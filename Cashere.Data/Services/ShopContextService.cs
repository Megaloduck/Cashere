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

    public async Task<SalesBehaviorSettings> GetSalesBehaviorSettingsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();

        return new SalesBehaviorSettings(
            settings?.RequireCustomerBeforeCheckout ?? false,
            settings?.AutoPrintReceiptAfterPayment ?? false);
    }

    public async Task<SecuritySettings> GetSecuritySettingsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();

        var timeoutMinutes = settings?.AutoLockTimeoutMinutes ?? 15;
        if (timeoutMinutes <= 0) timeoutMinutes = 15;

        return new SecuritySettings(settings?.AutoLockEnabled ?? false, timeoutMinutes);
    }
    public async Task<HeaderClockSettings> GetHeaderClockSettingsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();

        return new HeaderClockSettings(
            settings?.ShowHeaderClock ?? true,
            settings?.ShowHeaderDay ?? true,
            settings?.ShowHeaderDate ?? true,
            settings?.ShowHeaderMonth ?? true,
            settings?.ShowHeaderYear ?? true,
            settings?.ShowHeaderHours ?? true);
    }
}