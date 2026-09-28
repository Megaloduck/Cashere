using System;
using System.Collections.Generic;

namespace Cashere.Models;

// A named tax rate a Category can be assigned - see Category.TaxRateId. A
// product with no category, or a category with no TaxRate assigned, falls
// back to the shop-wide ReceiptAdmin.TaxRatePercent - see
// TaxCalculator.ResolveRatePercent, called identically by CartViewModel's
// live checkout preview and SaleService's authoritative computation, so the
// two can never resolve a different rate for the same line.
public class TaxRate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
