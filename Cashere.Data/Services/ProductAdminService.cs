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
    private readonly IProductCatalogChangeNotifier? _catalogChangeNotifier;

    public ProductAdminService(
        IDbContextFactory<CashereDbContext> dbContextFactory,
        IProductCatalogChangeNotifier? catalogChangeNotifier = null)
    {
        _dbContextFactory = dbContextFactory;
        _catalogChangeNotifier = catalogChangeNotifier;
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

        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();

        var sku = input.Sku;
        if (string.IsNullOrWhiteSpace(sku))
        {
            if (settings?.AutoGenerateSku != true)
            {
                throw new AdminValidationException("SKU is required.");
            }
            sku = await GenerateUniqueSkuAsync(db);
        }

        var barcode = NormalizeBarcode(input.Barcode);
        if (barcode is null && settings?.AutoGenerateBarcode == true)
        {
            barcode = await GenerateUniqueBarcodeAsync(db);
        }

        // Re-project through the record so downstream logic (uniqueness
        // check, entity creation) never has to know whether Sku/Barcode came
        // from the form or were just generated above.
        var effectiveInput = input with { Sku = sku, Barcode = barcode };

        await EnsureSkuAndBarcodeAreUniqueAsync(db, effectiveInput, existingProductId: null);

        var product = new Product
        {
            Sku = effectiveInput.Sku.Trim(),
            Barcode = effectiveInput.Barcode,
            Name = effectiveInput.Name.Trim(),
            CategoryId = effectiveInput.CategoryId,
            Unit = effectiveInput.Unit.Trim(),
            CostPrice = effectiveInput.CostPrice,
            SellingPrice = effectiveInput.SellingPrice,
            StockQuantity = effectiveInput.StockQuantity,
            LowStockThreshold = effectiveInput.LowStockThreshold,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        await NotifyCatalogChangedAsync();
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
        await NotifyCatalogChangedAsync();
    }

    public async Task SetActiveAsync(int productId, bool isActive)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new AdminValidationException($"Product {productId} was not found.");

        product.IsActive = isActive;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await NotifyCatalogChangedAsync();
    }

    public async Task SetPhotoPathAsync(int productId, string? photoPath)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new AdminValidationException($"Product {productId} was not found.");

        product.PhotoPath = string.IsNullOrWhiteSpace(photoPath) ? null : photoPath.Trim();
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await NotifyCatalogChangedAsync();
    }

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

    // Sequential-looking (SKU-000042), falling back to a timestamp if the
    // count-based guess somehow collides five times in a row.
    private static async Task<string> GenerateUniqueSkuAsync(CashereDbContext db)
    {
        var baseCount = await db.Products.CountAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"SKU-{baseCount + 1 + attempt:D6}";
            if (!await db.Products.AnyAsync(p => p.Sku == candidate))
            {
                return candidate;
            }
        }
        return $"SKU-{DateTime.UtcNow:yyMMddHHmmssfff}";
    }

    private static async Task<string> GenerateUniqueBarcodeAsync(CashereDbContext db)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = GenerateEan13InternalUseBarcode();
            if (!await db.Products.AnyAsync(p => p.Barcode == candidate))
            {
                return candidate;
            }
        }
        return GenerateEan13InternalUseBarcode();
    }

    // GS1 reserves the "20"-"29" prefix range for internal/in-store use, so
    // this is safe to generate locally without a real GS1 company prefix -
    // it's still a valid EAN-13 (correct check digit), just not globally
    // unique the way a purchased barcode would be.
    private static string GenerateEan13InternalUseBarcode()
    {
        var random = Random.Shared;
        var digits = new int[12];
        digits[0] = 2;
        for (var i = 1; i < 12; i++)
        {
            digits[i] = random.Next(0, 10);
        }

        var checkDigit = ComputeEan13CheckDigit(digits);

        var sb = new StringBuilder(13);
        foreach (var digit in digits) sb.Append(digit);
        sb.Append(checkDigit);
        return sb.ToString();
    }

    private static int ComputeEan13CheckDigit(int[] first12Digits)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += first12Digits[i] * (i % 2 == 0 ? 1 : 3);
        }
        var mod = sum % 10;
        return mod == 0 ? 0 : 10 - mod;
    }

    // Best-effort push to any connected mobile clients so their Labeling
    // product list picks up the change without needing a manual refresh or
    // reconnect. A failure here never fails the admin operation itself - a
    // phone that misses the push will still catch up on its own manual
    // refresh or next reconnect.
    private async Task NotifyCatalogChangedAsync()
    {
        if (_catalogChangeNotifier is null) return;

        try
        {
            await _catalogChangeNotifier.NotifyChangedAsync();
        }
        catch
        {
        }
    }
}