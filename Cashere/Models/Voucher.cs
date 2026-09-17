using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Models;

public enum VoucherDiscountType
{
    FixedAmount,
    Percentage
}

public class Voucher
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public VoucherDiscountType DiscountType { get; set; } = VoucherDiscountType.FixedAmount;
    public decimal DiscountValue { get; set; }
    // Null = unlimited redemptions (a reusable "house" code); 1 = single-use;
    // any other positive number = a capped-reuse code (e.g. "first 50 customers").
    public int? MaxUsageCount { get; set; }
    public int UsageCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<VoucherRedemption> Redemptions { get; set; } = new List<VoucherRedemption>();
}