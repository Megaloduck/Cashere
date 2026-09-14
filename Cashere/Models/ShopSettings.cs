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

    public string? PrinterName { get; set; }
    public int PrinterPaperWidthMm { get; set; } = 80;

    // Read by CheckoutViewModel (gates Complete Sale on a customer pick) and
    // re-checked by SaleService at completion; and by PosViewModel right
    // after a sale completes (fires a best-effort print through whatever
    // Settings -> Hardware has configured).
    public bool RequireCustomerBeforeCheckout { get; set; }
    public bool AutoPrintReceiptAfterPayment { get; set; }

    public string ServerBindAddress { get; set; } = "0.0.0.0";
    public int ServerPort { get; set; } = 5177;
}