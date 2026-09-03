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
    // Captured at sale time so profit reports stay accurate even if the product's cost changes later.
    public decimal UnitCostAtSale { get; set; }
    public decimal Subtotal { get; set; }
}
