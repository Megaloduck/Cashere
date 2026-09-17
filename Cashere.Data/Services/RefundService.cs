using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class RefundService : IRefundService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;
    private readonly IProductCatalogChangeNotifier? _catalogChangeNotifier;

    public RefundService(
        IDbContextFactory<CashereDbContext> dbContextFactory,
        IProductCatalogChangeNotifier? catalogChangeNotifier = null)
    {
        _dbContextFactory = dbContextFactory;
        _catalogChangeNotifier = catalogChangeNotifier;
    }

    public async Task<RefundableSaleDetail?> GetRefundableSaleAsync(int saleId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var sale = await db.Sales
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Items).ThenInclude(i => i.RefundLineItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null) return null;

        var lines = sale.Items.Select(i =>
        {
            var alreadyRefunded = i.RefundLineItems.Sum(l => l.Quantity);
            return new RefundLineDetail(
                i.Id, i.Product.Name, i.Quantity, alreadyRefunded,
                Math.Max(0, i.Quantity - alreadyRefunded), i.UnitPrice);
        }).ToList();

        return new RefundableSaleDetail(sale.Id, sale.SaleNumber, sale.Status, lines);
    }

    public async Task<RefundResult> RefundLinesAsync(
        int saleId, int processedByCashierId, IReadOnlyList<RefundLineInput> lines, string? reason)
    {
        if (lines.Count == 0)
        {
            throw new AdminValidationException("Select at least one line to refund.");
        }

        return await ProcessRefundAsync(saleId, processedByCashierId, lines, reason, isVoid: false);
    }

    public async Task<RefundResult> VoidSaleAsync(int saleId, int processedByCashierId, string? reason)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var sale = await db.Sales
            .Include(s => s.Items).ThenInclude(i => i.RefundLineItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == saleId)
            ?? throw new AdminValidationException($"Sale {saleId} was not found.");

        if (sale.Status == SaleStatus.Voided)
        {
            throw new AdminValidationException("This sale has already been voided.");
        }

        var remainingLines = sale.Items
            .Select(i => new RefundLineInput(i.Id, i.Quantity - i.RefundLineItems.Sum(l => l.Quantity)))
            .Where(l => l.Quantity > 0)
            .ToList();

        if (remainingLines.Count == 0)
        {
            throw new AdminValidationException("Nothing left on this sale to void - it's already fully refunded.");
        }

        return await ProcessRefundAsync(saleId, processedByCashierId, remainingLines, reason, isVoid: true);
    }

    private async Task<RefundResult> ProcessRefundAsync(
        int saleId, int processedByCashierId, IReadOnlyList<RefundLineInput> lines, string? reason, bool isVoid)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var sale = await db.Sales
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Items).ThenInclude(i => i.RefundLineItems)
            .FirstOrDefaultAsync(s => s.Id == saleId)
            ?? throw new AdminValidationException($"Sale {saleId} was not found.");

        if (sale.Status == SaleStatus.Voided)
        {
            throw new AdminValidationException("This sale has already been voided.");
        }

        var saleItemsById = sale.Items.ToDictionary(i => i.Id);

        var refund = new Refund
        {
            SaleId = sale.Id,
            ProcessedByCashierId = processedByCashierId,
            RefundDate = DateTime.UtcNow,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            IsVoid = isVoid
        };

        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                throw new AdminValidationException("Refund quantity must be greater than zero for every line.");
            }

            if (!saleItemsById.TryGetValue(line.SaleItemId, out var saleItem))
            {
                throw new AdminValidationException($"Sale line {line.SaleItemId} does not belong to this sale.");
            }

            var alreadyRefunded = saleItem.RefundLineItems.Sum(l => l.Quantity);
            var refundable = saleItem.Quantity - alreadyRefunded;

            if (line.Quantity > refundable)
            {
                throw new AdminValidationException(
                    $"Only {refundable} of '{saleItem.Product.Name}' can still be refunded.");
            }

            refund.Lines.Add(new RefundLineItem
            {
                SaleItemId = saleItem.Id,
                Quantity = line.Quantity,
                UnitPrice = saleItem.UnitPrice,
                Subtotal = saleItem.UnitPrice * line.Quantity
            });

            // Mirrors PurchaseAdminService's stock-received path, just for a
            // returned item instead of a newly purchased one.
            saleItem.Product.StockQuantity += line.Quantity;
            saleItem.Product.UpdatedAt = DateTime.UtcNow;

            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = saleItem.Product.Id,
                MovementType = InventoryMovementType.Return,
                QuantityChange = line.Quantity,
                ReferenceType = isVoid ? "SaleVoided" : "SaleRefunded",
                ReferenceId = sale.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        refund.TotalAmount = refund.Lines.Sum(l => l.Subtotal);
        db.Refunds.Add(refund);

        // Re-evaluate against every line on the sale (not just the ones just
        // touched) so a second, later refund on the same sale still lands
        // on the right status.
        var totalRefundedAcrossSale = sale.Items.Sum(i =>
            i.RefundLineItems.Sum(l => l.Quantity) +
            refund.Lines.Where(l => l.SaleItemId == i.Id).Sum(l => l.Quantity));
        var totalOriginalQuantity = sale.Items.Sum(i => i.Quantity);

        sale.Status = isVoid
            ? SaleStatus.Voided
            : totalRefundedAcrossSale >= totalOriginalQuantity
                ? SaleStatus.Refunded
                : SaleStatus.PartiallyRefunded;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

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

        return new RefundResult(refund.Id, refund.TotalAmount, sale.Status);
    }
}