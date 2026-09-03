namespace Cashere.Models;

// Single-row table holding shop-wide configuration (name, receipt footer, tax rate, etc).
public class ShopSettings
{
    public int Id { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string Currency { get; set; } = "IDR";
    public decimal TaxRatePercent { get; set; }
    public string? ReceiptFooterText { get; set; }
}
