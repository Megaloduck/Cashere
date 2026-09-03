using System;
using System.Collections.Generic;

namespace Cashere.Models;

// Audit trail of every stock change, whatever caused it.
public class InventoryMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public InventoryMovementType MovementType { get; set; }
    public int QuantityChange { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
