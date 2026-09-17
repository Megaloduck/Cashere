using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Models;

public class RefundLineItem
{
    public int Id { get; set; }
    public int RefundId { get; set; }
    public Refund Refund { get; set; } = null!;
    public int SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public int Quantity { get; set; }
    // Snapshot from the original SaleItem at refund time, same philosophy
    // as SaleItem.UnitCostAtSale freezing cost.
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}