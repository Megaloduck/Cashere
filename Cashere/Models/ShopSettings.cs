namespace Cashere.Models;

// Single-row table holding shop-wide configuration (name, receipt footer, tax rate, etc).
public class ReceiptAdmin
{
    public int Id { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string Currency { get; set; } = "IDR";
    public decimal TaxRatePercent { get; set; }
    public string? ReceiptFooterText { get; set; }

    // Local sync server config, read by Cashere.Desktop/Program.cs at
    // startup to configure Kestrel. "0.0.0.0" means "bind every network
    // adapter" - the safe default. Changing either requires an app restart
    // since Kestrel is already bound by the time this screen is editable.
    public string ServerBindAddress { get; set; } = "0.0.0.0";
    public int ServerPort { get; set; } = 5177;
}