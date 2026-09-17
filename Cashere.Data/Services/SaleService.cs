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

        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();

        var trackInventory = settings?.TrackInventory ?? true;
        var outOfStockBehavior = settings?.OutOfStockBehavior ?? OutOfStockBehavior.Block;
        var enforceStock = trackInventory && outOfStockBehavior == OutOfStockBehavior.Block;

        var methodEnabled = request.PaymentMethod switch
        {
            PaymentMethod.Cash => settings?.CashEnabled ?? true,
            PaymentMethod.Qris => settings?.QrisEnabled ?? true,
            PaymentMethod.Edc => settings?.EdcEnabled ?? true,
            _ => true
        };
        if (!methodEnabled)
        {
            throw new InvalidOperationException(
                $"{request.PaymentMethod} is currently disabled in Settings -> Payments.");
        }

        if ((settings?.RequireCustomerBeforeCheckout ?? false) && request.CustomerId is null)
        {
            throw new InvalidOperationException(
                "A customer is required before completing this sale (Settings -> Sales Behavior).");
        }

        var productIds = request.Lines.Select(l => l.ProductId).ToList();
        var productList = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        var products = productList.ToDictionary(p => p.Id);

        foreach (var line in request.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                throw new InvalidOperationException($"Product {line.ProductId} was not found.");
            }

            if (enforceStock && product.StockQuantity < line.Quantity)
            {
                throw new InsufficientStockException(product.Name, line.Quantity, product.StockQuantity);
            }
        }

        var subtotal = request.Lines.Sum(l => l.UnitPrice * l.Quantity);

        // Server-side re-validation, same defense-in-depth spirit as the
        // stock check above: the cart already previewed this discount, but
        // the voucher could have expired or been used up by another till in
        // the gap between preview and this commit, so the authoritative
        // amount is always computed fresh here, never trusted from the UI.
        Voucher? voucher = null;
        var effectiveDiscount = request.DiscountAmount;

        if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            var normalizedCode = request.VoucherCode.Trim().ToUpperInvariant();
            voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Code == normalizedCode);

            if (voucher is null)
            {
                throw new InvalidVoucherException(normalizedCode, "That voucher code doesn't exist.");
            }
            if (!voucher.IsActive)
            {
                throw new InvalidVoucherException(normalizedCode, "This voucher is no longer active.");
            }
            if (voucher.ExpiresAt is not null && voucher.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidVoucherException(normalizedCode, "This voucher has expired.");
            }
            if (voucher.MaxUsageCount is int max && voucher.UsageCount >= max)
            {
                throw new InvalidVoucherException(normalizedCode, "This voucher has already been fully redeemed.");
            }

            effectiveDiscount = VoucherAdminService.ComputeDiscount(voucher.DiscountType, voucher.DiscountValue, subtotal);
        }

        var taxAmount = Math.Round(
            (subtotal - effectiveDiscount) * (request.TaxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);
        var totalAmount = subtotal - effectiveDiscount + taxAmount;

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
            DiscountAmount = effectiveDiscount,
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

        if (voucher is not null)
        {
            voucher.UsageCount += 1;
            db.VoucherRedemptions.Add(new VoucherRedemption
            {
                VoucherId = voucher.Id,
                SaleId = sale.Id,
                DiscountAmount = effectiveDiscount,
                RedeemedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var changeDue = request.PaymentMethod == PaymentMethod.Cash
            ? Math.Max(0, request.AmountTendered - totalAmount)
            : 0;

        return new CompletedSaleResult(
            sale.Id, sale.SaleNumber, subtotal, effectiveDiscount, taxAmount, totalAmount, changeDue, sale.SaleDate);
    }
}