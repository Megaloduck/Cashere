using System;
using System.Collections.Generic;

namespace Cashere.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Null = falls back to the shop-wide ReceiptAdmin.TaxRatePercent - see
    // TaxCalculator. Deliberately per-category rather than per-product: the
    // smallest change that still lets a shop separate, say, "Food" from
    // "Non-Food" or an exempt category, without adding a picker to every
    // single product.
    public int? TaxRateId { get; set; }
    public TaxRate? TaxRate { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
