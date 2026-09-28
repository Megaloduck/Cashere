using System;
using System.Collections.Generic;
using System.Linq;
using Cashere.Models;

namespace Cashere.Services;

// Pure tax/rounding math shared by CartViewModel's live checkout preview and
// SaleService's authoritative computation at sale completion - the same
// "one calculation, two callers" split VoucherAdminService.ComputeDiscount
// already uses for voucher discounts, so the total shown at checkout can
// never drift from what SaleService actually charges.
public static class TaxCalculator
{
    public record LineInput(decimal LineSubtotal, decimal RatePercent);
    public record LineResult(decimal TaxAmount, decimal RatePercent);
    public record CalculationResult(IReadOnlyList<LineResult> Lines, decimal TotalTax);

    // Resolves the tax rate a product's line should use: its category's
    // assigned TaxRate if one is set, otherwise the shop-wide default from
    // Settings -> Business Info. Category is optional since not every
    // product has one (see Product.CategoryId).
    public static decimal ResolveRatePercent(Category? category, decimal defaultRatePercent) =>
        category?.TaxRate?.RatePercent ?? defaultRatePercent;

    // Splits a cart-level discount across lines pro-rata by subtotal share,
    // then computes each line's tax at its own rate - exclusive (added on
    // top of the price) or inclusive (backed out of a price that already
    // contains it), per Settings -> Business Info -> "Prices include tax".
    public static CalculationResult Calculate(
        IReadOnlyList<LineInput> lines, decimal discountAmount, bool pricesIncludeTax)
    {
        var subtotal = lines.Sum(l => l.LineSubtotal);
        var results = new List<LineResult>(lines.Count);
        decimal totalTax = 0;

        foreach (var line in lines)
        {
            var lineDiscount = subtotal > 0 ? discountAmount * (line.LineSubtotal / subtotal) : 0;
            var net = line.LineSubtotal - lineDiscount;
            var rateFraction = line.RatePercent / 100m;

            var lineTax = pricesIncludeTax
                ? Math.Round(net - net / (1 + rateFraction), 2, MidpointRounding.AwayFromZero)
                : Math.Round(net * rateFraction, 2, MidpointRounding.AwayFromZero);

            results.Add(new LineResult(lineTax, line.RatePercent));
            totalTax += lineTax;
        }

        return new CalculationResult(results, totalTax);
    }

    // Rounds a final total per Settings -> Business Info -> Rounding. A
    // RoundingIncrement <= 0 or RoundingMode.None both mean "leave it
    // alone" - callers don't need to special-case either.
    public static decimal ApplyRounding(decimal amount, RoundingMode mode, decimal increment)
    {
        if (mode == RoundingMode.None || increment <= 0) return amount;

        return mode switch
        {
            RoundingMode.Up => Math.Ceiling(amount / increment) * increment,
            RoundingMode.Nearest => Math.Round(amount / increment, MidpointRounding.AwayFromZero) * increment,
            _ => amount
        };
    }
}
