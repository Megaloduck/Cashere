using System;
using System.Collections.Generic;

namespace Cashere.Models;

public class Purchase
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public int CreatedByCashierId { get; set; }
    public Cashier CreatedByCashier { get; set; } = null!;
    public string? ReferenceNumber { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
