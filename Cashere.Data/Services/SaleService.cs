using System;
using System.Linq;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class SaleService : ISaleService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public SaleService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<CompletedSaleResult> CompleteSaleAsync(CompleteSaleRequest request)
    {
        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Cannot complete a sale with no items.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var productIds = request.Lines.Select(l => l.ProductId).ToList();
        var productList = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        var products = productList.ToDictionary(p => p.Id);

        // Validate everything up front before writing anything, so a bad line
        // never leaves a partial sale behind.
        foreach (var line in request.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                throw new InvalidOperationException($"Product {line.ProductId} was not found.");
            }

            if (product.StockQuantity < line.Quantity)
            {
                throw new InsufficientStockException(product.Name, line.Quantity, product.StockQuantity);
            }
        }

        var subtotal = request.Lines.Sum(l => l.UnitPrice * l.Quantity);
        var taxAmount = Math.Round(
            (subtotal - request.DiscountAmount) * (request.TaxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);
        var totalAmount = subtotal - request.DiscountAmount + taxAmount;

        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        var saleCountToday = await db.Sales.CountAsync(s => s.SaleDate >= todayUtc && s.SaleDate < tomorrowUtc);
        var saleNumber = $"S{DateTime.UtcNow:yyyyMMdd}-{saleCountToday + 1:D4}";

        var sale = new Sale
        {
            SaleNumber = saleNumber,
            CashierId = request.CashierId,
            CustomerId = request.CustomerId,
            SaleDate = DateTime.UtcNow,
            Subtotal = subtotal,
            DiscountAmount = request.DiscountAmount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Status = SaleStatus.Completed
        };

        foreach (var line in request.Lines)
        {
            var product = products[line.ProductId];
            sale.Items.Add(new SaleItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                // Frozen at sale time so profit reports stay accurate even if
                // the product's cost changes later.
                UnitCostAtSale = product.CostPrice,
                Subtotal = line.UnitPrice * line.Quantity
            });
        }

        sale.Payments.Add(new Payment
        {
            Method = request.PaymentMethod,
            Amount = totalAmount,
            ReferenceNumber = request.PaymentReferenceNumber,
            PaidAt = DateTime.UtcNow
        });

        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        foreach (var line in request.Lines)
        {
            var product = products[line.ProductId];
            product.StockQuantity -= line.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id,
                MovementType = InventoryMovementType.SaleDeducted,
                QuantityChange = -line.Quantity,
                ReferenceType = "Sale",
                ReferenceId = sale.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var changeDue = request.PaymentMethod == PaymentMethod.Cash
            ? Math.Max(0, request.AmountTendered - totalAmount)
            : 0;

        return new CompletedSaleResult(
            sale.Id, sale.SaleNumber, subtotal, request.DiscountAmount, taxAmount, totalAmount, changeDue, sale.SaleDate);
    }
}
