using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services
{
    // TaxRatePercent moved here from CompleteSaleRequest: each line now
    // carries whatever rate CartViewModel resolved for it (its product's
    // Category.TaxRate, or the shop-wide default) via TaxCalculator, so a
    // sale can mix rates instead of applying one flat percentage to
    // everything. See SaleService.CompleteSaleAsync.
    public record SaleLineRequest(int ProductId, int Quantity, decimal UnitPrice, decimal TaxRatePercent);

    public record CompleteSaleRequest(
        int CashierId,
        int? CustomerId,
        IReadOnlyList<SaleLineRequest> Lines,
        decimal DiscountAmount,
        PaymentMethod PaymentMethod,
        decimal AmountTendered,
        string? PaymentReferenceNumber,
        // Optional - when set, SaleService re-validates and computes the
        // authoritative discount server-side, overriding whatever
        // DiscountAmount the UI cached. Null for a plain sale with no
        // voucher (DiscountAmount above is used as-is, unchanged from before
        // this field existed).
        string? VoucherCode = null);

    public record CompletedSaleResult(
        int SaleId,
        string SaleNumber,
        decimal Subtotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        // How much Settings -> Business Info -> Rounding changed the total
        // by - zero whenever rounding is off. See TaxCalculator.ApplyRounding.
        decimal RoundingAdjustment,
        decimal TotalAmount,
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
