using System.Collections.Generic;

namespace Cashere.Models;

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCostAtSale { get; set; }
    public decimal Subtotal { get; set; }

    // This line's share of Sale.TaxAmount, at whatever rate resolved for it
    // at sale time (its product's Category.TaxRate, or the shop-wide
    // default) - frozen here the same way UnitCostAtSale freezes cost, so a
    // later change to a category's rate never rewrites a past receipt. See
    // TaxCalculator.
    public decimal TaxAmount { get; set; }

    public ICollection<RefundLineItem> RefundLineItems { get; set; } = new List<RefundLineItem>();
}
