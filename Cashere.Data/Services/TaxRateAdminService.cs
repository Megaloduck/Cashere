using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class TaxRateAdminService : ITaxRateAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public TaxRateAdminService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<TaxRate>> GetAllTaxRatesAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.TaxRates.OrderBy(t => t.Name).AsNoTracking().ToListAsync();
    }

    public async Task<TaxRate> CreateTaxRateAsync(TaxRateInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var trimmed = input.Name.Trim();
        if (await db.TaxRates.AnyAsync(t => t.Name == trimmed))
        {
            throw new AdminValidationException($"A tax rate named '{trimmed}' already exists.");
        }

        var taxRate = new TaxRate { Name = trimmed, RatePercent = input.RatePercent };
        db.TaxRates.Add(taxRate);
        await db.SaveChangesAsync();
        return taxRate;
    }

    public async Task UpdateTaxRateAsync(int taxRateId, TaxRateInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var taxRate = await db.TaxRates.FirstOrDefaultAsync(t => t.Id == taxRateId)
            ?? throw new AdminValidationException($"Tax rate {taxRateId} was not found.");

        var trimmed = input.Name.Trim();
        if (await db.TaxRates.AnyAsync(t => t.Name == trimmed && t.Id != taxRateId))
        {
            throw new AdminValidationException($"A tax rate named '{trimmed}' already exists.");
        }

        taxRate.Name = trimmed;
        taxRate.RatePercent = input.RatePercent;
        await db.SaveChangesAsync();
    }

    public async Task DeleteTaxRateAsync(int taxRateId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var taxRate = await db.TaxRates.FirstOrDefaultAsync(t => t.Id == taxRateId);
        if (taxRate is null) return;

        // Categories reference this with OnDelete(SetNull) - they simply
        // fall back to the shop-wide default rate rather than blocking the
        // delete, same philosophy as ProductConfiguration's Category FK.
        db.TaxRates.Remove(taxRate);
        await db.SaveChangesAsync();
    }

    public async Task<List<Category>> GetCategoriesWithTaxRatesAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Categories.Include(c => c.TaxRate).OrderBy(c => c.Name).AsNoTracking().ToListAsync();
    }

    public async Task AssignCategoryTaxRateAsync(int categoryId, int? taxRateId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId)
            ?? throw new AdminValidationException($"Category {categoryId} was not found.");

        category.TaxRateId = taxRateId;
        await db.SaveChangesAsync();
    }
}
