using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services
{
    // TaxRatePercent lives on each line: every line carries whatever rate
    // CartViewModel resolved for it (its product's Category.TaxRate, or the
    // shop-wide default) via TaxCalculator, so a sale can mix rates. See
    // SaleService.CompleteSaleAsync.
    public record SaleLineRequest(int ProductId, int Quantity, decimal UnitPrice, decimal TaxRatePercent);

    // One line of a (possibly split) payment. Amount is how much of the sale
    // this line settles - for Cash, that's the *applied* portion, never the
    // amount physically handed over (that's CompleteSaleRequest.AmountTendered,
    // which SaleService uses only to work out change). FeeAmount is what the
    // client computed for preview purposes; SaleService recomputes it from
    // settings and never trusts this value - see SaleService.CompleteSaleAsync.
    public record PaymentInput(
        PaymentMethod Method,
        decimal Amount,
        decimal FeeAmount,
        string? ReferenceNumber);

    public record CompleteSaleRequest(
        int CashierId,
        int? CustomerId,
        IReadOnlyList<SaleLineRequest> Lines,
        decimal DiscountAmount,
        // Replaces the old single PaymentMethod/PaymentReferenceNumber pair.
        // Must contain at least one entry, and the sum of Amount across all
        // entries must equal the sale total - SaleService re-validates both.
        IReadOnlyList<PaymentInput> Payments,
        // Total physical cash handed over across every Cash payment line -
        // used only to compute change (AmountTendered minus the sum of cash
        // Amounts). Ignored (and treated as zero) if no Cash line is present.
        decimal AmountTendered,
        // Optional - when set, SaleService re-validates and computes the
        // authoritative discount server-side, overriding whatever
        // DiscountAmount the UI cached. Null for a plain sale with no
        // voucher (DiscountAmount above is used as-is, unchanged from before
        // this field existed).
        string? VoucherCode = null,
        string OrderType = "Sale");

    public record CompletedSaleResult(
        int SaleId,
        string SaleNumber,
        decimal Subtotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        // How much Settings -> Payments -> Rounding changed the total
        // by - zero whenever rounding is off. See TaxCalculator.ApplyRounding.
        decimal RoundingAdjustment,
        decimal TotalAmount,
        // Sum of every payment line's fee (Settings -> Payments -> per-method
        // fee). Informational - not part of TotalAmount.
        decimal TotalFees,
        decimal ChangeDue,
        DateTime SaleDate);

    // Thrown when a line's requested quantity exceeds what's currently in stock -
    // the ViewModel catches this and surfaces it as a friendly validation message
    // rather than a generic failure.
    public class InsufficientStockException : Exception
    {
        public string ProductName { get; }
        public int Requested { get; }
        public int Available { get; }

        public InsufficientStockException(string productName, int requested, int available)
            : base($"Not enough stock for '{productName}': requested {requested}, only {available} available.")
        {
            ProductName = productName;
            Requested = requested;
            Available = available;
        }
    }

    // Thrown when a voucher code passed to CompleteSaleAsync fails
    // server-side re-validation (doesn't exist, inactive, expired, or its
    // usage cap was hit by someone else between the cart preview and
    // checkout) - CheckoutViewModel catches this the same way it catches
    // InsufficientStockException.
    public class InvalidVoucherException : Exception
    {
        public string Code { get; }

        public InvalidVoucherException(string code, string reason) : base(reason)
        {
            Code = code;
        }
    }

    public interface ISaleService
    {
        Task<CompletedSaleResult> CompleteSaleAsync(CompleteSaleRequest request);
    }
}
