using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class PurchaseAdminService : IPurchaseAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;
    private readonly IProductCatalogChangeNotifier? _catalogChangeNotifier;

    public PurchaseAdminService(
        IDbContextFactory<CashereDbContext> dbContextFactory,
        IProductCatalogChangeNotifier? catalogChangeNotifier = null)
    {
        _dbContextFactory = dbContextFactory;
        _catalogChangeNotifier = catalogChangeNotifier;
    }

    public async Task<List<Purchase>> GetAllPurchasesAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .OrderByDescending(p => p.PurchaseDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Purchase> CreatePurchaseAsync(PurchaseInput input)
    {
        if (input.Lines.Count == 0)
        {
            throw new AdminValidationException("A purchase needs at least one line item.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == input.SupplierId)
            ?? throw new AdminValidationException("Select a valid supplier.");

        var productIds = input.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        foreach (var line in input.Lines)
        {
            if (!products.ContainsKey(line.ProductId))
            {
                throw new AdminValidationException($"Product {line.ProductId} was not found.");
            }

            if (line.Quantity <= 0)
            {
                throw new AdminValidationException("Quantity must be greater than zero for every line.");
            }
        }

        var purchase = new Purchase
        {
            SupplierId = supplier.Id,
            CreatedByCashierId = input.CreatedByCashierId,
            ReferenceNumber = string.IsNullOrWhiteSpace(input.ReferenceNumber) ? null : input.ReferenceNumber.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            PurchaseDate = DateTime.UtcNow,
            TotalAmount = input.Lines.Sum(l => l.UnitCost * l.Quantity)
        };

        foreach (var line in input.Lines)
        {
            purchase.Items.Add(new PurchaseItem
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                Subtotal = line.UnitCost * line.Quantity
            });
        }

        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();

        // Stock update + audit trail mirrors SaleService's post-sale deduction,
        // just adding instead of subtracting.
        foreach (var line in input.Lines)
        {
            var product = products[line.ProductId];
            product.StockQuantity += line.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id,
                MovementType = InventoryMovementType.PurchaseReceived,
                QuantityChange = line.Quantity,
                ReferenceType = "Purchase",
                ReferenceId = purchase.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        // Purchases change Product.StockQuantity, which is part of the
        // mobile-facing ProductDto projection - push the same catalog-changed
        // signal ProductAdminService uses, best-effort.
        if (_catalogChangeNotifier is not null)
        {
            try
            {
                await _catalogChangeNotifier.NotifyChangedAsync();
            }
            catch
            {
            }
        }

        return purchase;
    }
}