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

        if (request.Payments.Count == 0)
        {
            throw new InvalidOperationException("Cannot complete a sale with no payment.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();

        var trackInventory = settings?.TrackInventory ?? true;
        var outOfStockBehavior = settings?.OutOfStockBehavior ?? OutOfStockBehavior.Block;
        var enforceStock = trackInventory && outOfStockBehavior == OutOfStockBehavior.Block;

        // Built once from the freshly-read settings row so method-enabled
        // checks and fee computation below go through the exact same
        // PaymentSettings logic CheckoutViewModel uses for its preview.
        var paymentSettings = new PaymentSettings(
            settings?.CashEnabled ?? true,
            settings?.QrisEnabled ?? true,
            settings?.EdcEnabled ?? true,
            settings?.QrisAccountInfo,
            settings?.EdcAccountInfo,
            settings?.RequireConfirmationForNonCash ?? false,
            settings?.CashFeePercent ?? 0,
            settings?.QrisFeePercent ?? 0,
            settings?.EdcFeePercent ?? 0);

        foreach (var method in request.Payments.Select(p => p.Method).Distinct())
        {
            if (!paymentSettings.IsMethodEnabled(method))
            {
                throw new InvalidOperationException(
                    $"{method} is currently disabled in Settings -> Payments.");
            }
        }

        if ((settings?.RequireCustomerBeforeCheckout ?? false) && request.CustomerId is null)
        {
            throw new InvalidOperationException(
                "A customer is required before completing this sale (Settings -> Sales Behavior).");
        }

        var productIds = request.Lines.Select(l => l.ProductId).ToList();
        var productList = await db.Products
            .Include(p => p.Category)
                .ThenInclude(c => c!.TaxRate)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();
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

        // Tax is computed per line by TaxCalculator - the same function
        // CartViewModel's live preview uses - at whatever rate each line
        // carries (its product's Category.TaxRate, or the shop-wide
        // default at the time it was added to the cart). PricesIncludeTax
        // and rounding are deliberately re-read fresh from settings here
        // rather than trusted from the request, the same "server re-checks
        // what the UI already gated on" pattern as CashEnabled and
        // RequireCustomerBeforeCheckout above.
        var pricesIncludeTax = settings?.PricesIncludeTax ?? false;
        var taxInputs = request.Lines
            .Select(l => new TaxCalculator.LineInput(l.UnitPrice * l.Quantity, l.TaxRatePercent))
            .ToList();
        var taxCalculation = TaxCalculator.Calculate(taxInputs, effectiveDiscount, pricesIncludeTax);
        var taxAmount = taxCalculation.TotalTax;

        var preRoundingTotal = pricesIncludeTax
            ? subtotal - effectiveDiscount
            : subtotal - effectiveDiscount + taxAmount;

        var roundingMode = settings?.RoundingMode ?? RoundingMode.None;
        var roundingIncrement = settings?.RoundingIncrement ?? 0;
        var totalAmount = TaxCalculator.ApplyRounding(preRoundingTotal, roundingMode, roundingIncrement);
        var roundingAdjustment = totalAmount - preRoundingTotal;

        // The payment lines must add up to exactly the authoritative total
        // computed just above - not whatever total the UI thought it
        // previewed, which could have drifted (a price/voucher/rate changed
        // mid-checkout). A cent of slack absorbs decimal rounding noise, no
        // more. Anything else means the cashier would be settling the wrong
        // amount, so refuse rather than silently record a mismatched sale.
        var paymentsTotal = request.Payments.Sum(p => p.Amount);
        if (Math.Abs(paymentsTotal - totalAmount) > 0.01m)
        {
            throw new InvalidOperationException(
                $"Payments total Rp {paymentsTotal:N0} doesn't match the sale total of Rp {totalAmount:N0} - " +
                "the cart may have changed. Cancel and try again.");
        }

        if (request.Payments.Any(p => p.Amount <= 0))
        {
            throw new InvalidOperationException("Every payment line must be for more than zero.");
        }

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
            RoundingAdjustment = roundingAdjustment,
            TotalAmount = totalAmount,
            Status = SaleStatus.Completed
        };

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            var product = products[line.ProductId];
            sale.Items.Add(new SaleItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                UnitCostAtSale = product.CostPrice,
                Subtotal = line.UnitPrice * line.Quantity,
                TaxAmount = taxCalculation.Lines[i].TaxAmount
            });
        }

        // One Payment row per line the cashier added. FeeAmount is always
        // recomputed here from the fee % just read from settings - the
        // client-supplied PaymentInput.FeeAmount is only a preview and is
        // deliberately ignored, same "never trust the client's number"
        // philosophy as the voucher discount above.
        decimal totalFees = 0;
        foreach (var payment in request.Payments)
        {
            var feeAmount = paymentSettings.ComputeFee(payment.Method, payment.Amount);
            totalFees += feeAmount;

            sale.Payments.Add(new Payment
            {
                Method = payment.Method,
                Amount = payment.Amount,
                FeeAmount = feeAmount,
                ReferenceNumber = payment.Method == PaymentMethod.Cash ? null : payment.ReferenceNumber,
                PaidAt = DateTime.UtcNow
            });
        }

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

        // Change only ever comes from cash: physically-handed-over cash
        // minus the portion of the sale that cash lines actually settled.
        // Non-cash lines can't overpay, so they never contribute change.
        var cashApplied = request.Payments
            .Where(p => p.Method == PaymentMethod.Cash)
            .Sum(p => p.Amount);
        var changeDue = cashApplied > 0
            ? Math.Max(0, request.AmountTendered - cashApplied)
            : 0;

        return new CompletedSaleResult(
            sale.Id, sale.SaleNumber, subtotal, effectiveDiscount, taxAmount, roundingAdjustment, totalAmount,
            totalFees, changeDue, sale.SaleDate);
    }
}