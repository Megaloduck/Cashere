using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Models;

// One row per sale a voucher was applied to - the audit trail behind
// Voucher.UsageCount, mirroring how Refund/RefundLineItem back a sale's
// refund history. DiscountAmount is frozen at redemption time (a percentage
// voucher's Rp value depends on that sale's subtotal, same "freeze the
// number, not the formula" philosophy as SaleItem.UnitCostAtSale).
public class VoucherRedemption
{
    public int Id { get; set; }
    public int VoucherId { get; set; }
    public Voucher Voucher { get; set; } = null!;
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public decimal DiscountAmount { get; set; }
    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
}