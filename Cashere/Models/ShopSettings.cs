namespace Cashere.Models;

// Single-row table holding shop-wide configuration (name, receipt footer, tax rate, etc).
public class ReceiptAdmin
{
    public int Id { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxId { get; set; }
    public string Currency { get; set; } = "IDR";
    public decimal TaxRatePercent { get; set; }
    public string? ReceiptFooterText { get; set; }
    public string? Timezone { get; set; }

    public bool TrackInventory { get; set; } = true;
    public OutOfStockBehavior OutOfStockBehavior { get; set; } = OutOfStockBehavior.Block;
    public int DefaultLowStockThreshold { get; set; } = 5;
    public bool AutoGenerateSku { get; set; }
    public bool AutoGenerateBarcode { get; set; }

    public bool CashEnabled { get; set; } = true;
    public bool QrisEnabled { get; set; } = true;
    public bool EdcEnabled { get; set; } = true;
    public string? QrisAccountInfo { get; set; }
    public string? EdcAccountInfo { get; set; }
    public bool RequireConfirmationForNonCash { get; set; }

    // Receipt printer selection & paper width - read by
    // WindowsReceiptPrinterService on every PrintAsync call. Null
    // PrinterName means "use whatever Windows has set as its own default
    // printer" - the exact behavior this app always had before this
    // setting existed, so an empty/never-visited Hardware screen changes
    // nothing.
    public string? PrinterName { get; set; }
    public int PrinterPaperWidthMm { get; set; } = 80;

    public string ServerBindAddress { get; set; } = "0.0.0.0";
    public int ServerPort { get; set; } = 5177;
}