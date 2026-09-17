using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;

namespace Cashere.Models;

// A single refund/void event against a completed sale - may cover one or
// more lines at partial or full quantity. VoidSaleAsync creates one of
// these covering every remaining line at full quantity in one shot;
// RefundLinesAsync creates one covering whatever the cashier picked.
// Multiple Refund rows can exist against the same Sale over time -
// RefundedQuantity per line is always derived by summing
// RefundLineItem.Quantity across every Refund tied to that SaleItem, never
// stored redundantly on the line itself.
public class Refund
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int ProcessedByCashierId { get; set; }
    public Cashier ProcessedByCashier { get; set; } = null!;
    public DateTime RefundDate { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    // True only for the one-click VOID action - lets Sales History show
    // "VOIDED" on the specific event that cancelled the sale outright,
    // distinct from an ordinary (partial or full) refund of the same sale.
    public bool IsVoid { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<RefundLineItem> Lines { get; set; } = new List<RefundLineItem>();
}