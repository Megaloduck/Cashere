using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class ProductCatalogService : IProductCatalogService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public ProductCatalogService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Product>> GetActiveProductsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Categories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> FindByBarcodeAsync(string barcode)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
    }
}
