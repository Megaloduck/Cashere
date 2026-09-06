using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class ProductAdminService : IProductAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public ProductAdminService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Products
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product> CreateProductAsync(ProductInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        await EnsureSkuAndBarcodeAreUniqueAsync(db, input, existingProductId: null);

        var product = new Product
        {
            Sku = input.Sku.Trim(),
            Barcode = NormalizeBarcode(input.Barcode),
            Name = input.Name.Trim(),
            CategoryId = input.CategoryId,
            Unit = input.Unit.Trim(),
            CostPrice = input.CostPrice,
            SellingPrice = input.SellingPrice,
            StockQuantity = input.StockQuantity,
            LowStockThreshold = input.LowStockThreshold,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    public async Task UpdateProductAsync(int productId, ProductInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new AdminValidationException($"Product {productId} was not found.");

        await EnsureSkuAndBarcodeAreUniqueAsync(db, input, existingProductId: productId);

        product.Sku = input.Sku.Trim();
        product.Barcode = NormalizeBarcode(input.Barcode);
        product.Name = input.Name.Trim();
        product.CategoryId = input.CategoryId;
        product.Unit = input.Unit.Trim();
        product.CostPrice = input.CostPrice;
        product.SellingPrice = input.SellingPrice;
        product.StockQuantity = input.StockQuantity;
        product.LowStockThreshold = input.LowStockThreshold;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int productId, bool isActive)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new AdminValidationException($"Product {productId} was not found.");

        product.IsActive = isActive;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // Checked up front rather than relying on catching the unique-index
    // violation, so the cashier gets a clear message instead of a raw
    // SQLite constraint error.
    private static async Task EnsureSkuAndBarcodeAreUniqueAsync(CashereDbContext db, ProductInput input, int? existingProductId)
    {
        var sku = input.Sku.Trim();
        var skuTaken = await db.Products.AnyAsync(p => p.Sku == sku && (existingProductId == null || p.Id != existingProductId));
        if (skuTaken)
        {
            throw new AdminValidationException($"SKU '{sku}' is already in use by another product.");
        }

        var barcode = NormalizeBarcode(input.Barcode);
        if (barcode is not null)
        {
            var barcodeTaken = await db.Products.AnyAsync(p => p.Barcode == barcode && (existingProductId == null || p.Id != existingProductId));
            if (barcodeTaken)
            {
                throw new AdminValidationException($"Barcode '{barcode}' is already assigned to another product.");
            }
        }
    }

    private static string? NormalizeBarcode(string? barcode) =>
        string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
}