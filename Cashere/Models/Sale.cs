namespace Cashere.Models;

public class Sale
{
    public int Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public int CashierId { get; set; }
    public Cashier Cashier { get; set; } = null!;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
