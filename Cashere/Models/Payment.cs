namespace Cashere.Models;

public class Payment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    // QRIS/EDC transaction reference; null for cash.
    public string? ReferenceNumber { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
