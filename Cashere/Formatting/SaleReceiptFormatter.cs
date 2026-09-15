using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Formatting;

public record SaleReceiptLine(string Name, int Quantity, decimal Subtotal);

public record SaleReceiptContext(
    string ShopName,
    string? Address,
    string? Phone,
    string Currency,
    DateTime SaleDate,
    string SaleNumber,
    string CashierName,
    IReadOnlyList<SaleReceiptLine> Lines,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal TotalAmount,
    bool IsCashPayment,
    string PaymentMethodLabel,
    decimal AmountTendered,
    decimal ChangeDue,
    string? ReferenceNumber,
    string? FooterText);

// Plain-text rendering of a completed sale, for the auto-print-after-payment
// path (Settings -> Sales Behavior). Deliberately separate from
// ReceiptAdminViewModel's own BuildReceiptText(), which renders a fixed
// *sample* basket for the Settings screen's preview - kept untouched to
// avoid risking that already-working feature. The two produce visually
// identical layouts by design; they just read from different data.
public static class SaleReceiptFormatter
{
    private const string Divider = "----------------------------------------";
    private const int Width = 40;

    public static string Format(SaleReceiptContext ctx)
    {
        var currency = string.IsNullOrWhiteSpace(ctx.Currency) ? "IDR" : ctx.Currency.Trim().ToUpperInvariant();
        var sb = new StringBuilder();

        sb.AppendLine(Center(ctx.ShopName));
        if (!string.IsNullOrWhiteSpace(ctx.Address)) sb.AppendLine(Center(ctx.Address));
        if (!string.IsNullOrWhiteSpace(ctx.Phone)) sb.AppendLine(Center(ctx.Phone));
        sb.AppendLine(Divider);

        var dateText = ctx.SaleDate.ToString("dd MMM yyyy HH:mm");
        sb.AppendLine($"{dateText,-20}{ctx.SaleNumber,20}");
        sb.AppendLine($"Cashier: {ctx.CashierName}");
        sb.AppendLine(Divider);

        foreach (var line in ctx.Lines)
        {
            sb.AppendLine(line.Name);
            sb.AppendLine($"  {line.Quantity} x{line.Subtotal,30:N0}");
        }

        sb.AppendLine(Divider);
        sb.AppendLine($"{"SUBTOTAL",-20}{$"{currency} {ctx.Subtotal:N0}",20}");
        if (ctx.DiscountAmount > 0)
        {
            sb.AppendLine($"{"DISCOUNT",-20}{$"{currency} {ctx.DiscountAmount:N0}",20}");
        }
        sb.AppendLine($"{$"TAX ({ctx.TaxRatePercent:0.##}%)",-20}{$"{currency} {ctx.TaxAmount:N0}",20}");
        sb.AppendLine($"{"TOTAL",-20}{$"{currency} {ctx.TotalAmount:N0}",20}");
        sb.AppendLine(Divider);

        if (ctx.IsCashPayment)
        {
            sb.AppendLine($"{"CASH",-20}{$"{currency} {ctx.AmountTendered:N0}",20}");
            sb.AppendLine($"{"CHANGE",-20}{$"{currency} {ctx.ChangeDue:N0}",20}");
        }
        else
        {
            sb.AppendLine($"Paid via {ctx.PaymentMethodLabel}");
            if (!string.IsNullOrWhiteSpace(ctx.ReferenceNumber))
            {
                sb.AppendLine($"Ref: {ctx.ReferenceNumber}");
            }
        }

        if (!string.IsNullOrWhiteSpace(ctx.FooterText))
        {
            sb.AppendLine(Divider);
            sb.AppendLine(Center(ctx.FooterText));
        }

        return sb.ToString();
    }

    private static string Center(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length >= Width) return text;
        var pad = (Width - text.Length) / 2;
        return new string(' ', pad) + text;
    }
}