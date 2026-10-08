using System;
using System.Collections.Generic;

namespace Cashere.Models;

// A named tax rate a Category can be assigned - see Category.TaxRateId.
// Product-specific percentage overrides take precedence over category
// rates; products without one use their category rate, then the shop-wide
// default.
public class TaxRate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
