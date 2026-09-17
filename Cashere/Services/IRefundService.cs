using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public record RefundLineInput(int SaleItemId, int Quantity);

public record RefundLineDetail(
    int SaleItemId, string ProductName, int OriginalQuantity,
    int AlreadyRefundedQuantity, int RefundableQuantity, decimal UnitPrice);

public record RefundableSaleDetail(int SaleId, string SaleNumber, SaleStatus Status, IReadOnlyList<RefundLineDetail> Lines);

public record RefundResult(int RefundId, decimal TotalAmount, SaleStatus NewSaleStatus);

// Mirrors ISaleService's shape (a service dedicated to one lifecycle
// action) rather than folding into ISalesReportService, which stays purely
// read-only, or ISaleService, which stays purely "complete a new sale".
public interface IRefundService
{
    // Loads the sale's lines annotated with how much of each has already
    // been refunded, so the UI can cap the quantity picker per line.
    Task<RefundableSaleDetail?> GetRefundableSaleAsync(int saleId);

    // Refunds specific lines/quantities - used for a genuine partial refund
    // (a subset) as well as a full refund via picking every remaining line.
    // Restocks each refunded quantity and logs an InventoryMovement.Return
    // per product, then sets Sale.Status to Refunded (nothing refundable
    // remains) or PartiallyRefunded (some does).
    Task<RefundResult> RefundLinesAsync(int saleId, int processedByCashierId, IReadOnlyList<RefundLineInput> lines, string? reason);

    // One-click cancel of the whole sale: refunds every remaining line at
    // its full remaining quantity in one Refund record and sets
    // Sale.Status to Voided specifically - distinct from
    // RefundLinesAsync's Refunded/PartiallyRefunded outcome.
    Task<RefundResult> VoidSaleAsync(int saleId, int processedByCashierId, string? reason);
}