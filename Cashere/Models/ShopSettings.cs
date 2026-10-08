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
    // The shop-wide default tax rate - used for any product whose Category
    // has no TaxRate assigned (or has no Category at all). See
    // Settings -> Payments -> Tax Rates and TaxCalculator.
    public decimal TaxRatePercent { get; set; }
    public string? ReceiptFooterText { get; set; }

    // Business Info's timezone preset. Displayed clocks remain local to each
    // device; this offset is used when checkout-hour enforcement is enabled.
    public string? Timezone { get; set; }

    // Settings -> Business Info: relative path (under the desktop's local
    // media folder, "branding" subfolder) to the shop's logo - shown on the
    // Business Info screen and the on-screen receipt preview. See
    // Converters/LogoPathToImageConverter, which mirrors
    // PhotoPathToImageConverter's "relative path resolved against a local
    // media root" pattern.
    public string? LogoPath { get; set; }

    // Settings -> Business Info: per-day opening hours, serialized as JSON -
    // a single flat column rather than a child table, since it's always
    // read/written as a whole week at once. See
    // Models/BusinessHours.cs (BusinessHoursSerializer) for the shape.
    public string? BusinessHoursJson { get; set; }
    public bool EnforceBusinessHoursAtCheckout { get; set; }

    // Settings -> Payments -> Rounding: rounds Sale.TotalAmount (after
    // discount and tax) to the nearest RoundingIncrement, e.g. 100, so cash
    // change never needs odd small denominations. RoundingIncrement <= 0 or
    // RoundingMode.None both mean "no rounding" - see
    // TaxCalculator.ApplyRounding, used identically by CartViewModel's live
    // preview and SaleService's authoritative total.
    public RoundingMode RoundingMode { get; set; } = RoundingMode.None;
    public decimal RoundingIncrement { get; set; }

    // Settings -> Payments: when true, Product.SellingPrice already has
    // tax baked into it and TaxCalculator backs the tax amount out of the
    // price instead of adding it on top. See TaxCalculator.Calculate.
    public bool PricesIncludeTax { get; set; }

    // Settings -> Preferences. Drives the live day/date/month/year/hours
    // display in the Pos/Admin shell headers - see HeaderClockService,
    // which is configured from these on login and re-applied live on Save.
    public bool ShowHeaderClock { get; set; } = true;
    public bool ShowHeaderDay { get; set; } = true;
    public bool ShowHeaderDate { get; set; } = true;
    public bool ShowHeaderMonth { get; set; } = true;
    public bool ShowHeaderYear { get; set; } = true;
    public bool ShowHeaderHours { get; set; } = true;

    public bool TrackInventory { get; set; } = true;
    public OutOfStockBehavior OutOfStockBehavior { get; set; } = OutOfStockBehavior.Block;
    public int DefaultLowStockThreshold { get; set; } = 5;
    public bool AutoGenerateSku { get; set; }
    public bool AutoGenerateBarcode { get; set; }
    public string SkuPrefix { get; set; } = "SKU";
    public int SkuNumberLength { get; set; } = 6;
    public string InternalBarcodePrefix { get; set; } = "20";

    public bool CashEnabled { get; set; } = true;
    public bool QrisEnabled { get; set; } = true;
    public bool EdcEnabled { get; set; } = true;
    public string? QrisAccountInfo { get; set; }
    public string? EdcAccountInfo { get; set; }
    public bool RequireConfirmationForNonCash { get; set; }
    public decimal CashFeePercent { get; set; }
    public decimal QrisFeePercent { get; set; }
    public decimal EdcFeePercent { get; set; }

    public string? PrinterName { get; set; }
    public int PrinterPaperWidthMm { get; set; } = 80;

    public bool RequireCustomerBeforeCheckout { get; set; }
    public bool AutoPrintReceiptAfterPayment { get; set; }
    public bool EnableHeldOrders { get; set; } = true;
    public string SaleNumberPrefix { get; set; } = "S";
    public bool IncludeDateInSaleNumber { get; set; } = true;
    public int SaleNumberSequenceDigits { get; set; } = 4;
    public bool ScheduledBackupsEnabled { get; set; }
    public string ScheduledBackupTime { get; set; } = "23:00";
    public int AutomaticBackupRetentionCount { get; set; } = 30;
    public bool CashierCanViewOwnSalesHistory { get; set; }
    public bool CashierCanRequestRefunds { get; set; }
    public bool CashierCanRequestVoids { get; set; }
    public bool CashierCanApplyVouchers { get; set; } = true;
    public bool AutoAddScannedBarcode { get; set; } = true;
    public bool EnableCustomerDisplay { get; set; }
    public bool EnableCashDrawerKick { get; set; }
    // Settings -> Sales Behavior. A comma-separated, owner-configured list
    // keeps order types optional for simple retail and supports shop-specific
    // labels (for example Dine-in, Takeaway, Delivery) without imposing them.
    public string OrderTypes { get; set; } = "Sale";

    // Settings -> Security. Enforced by AutoLockService, configured from
    // RootViewModel right after login and re-applied live on Save - same
    // "takes effect immediately" feel as ThemeMode below.
    public bool AutoLockEnabled { get; set; }
    public int AutoLockTimeoutMinutes { get; set; } = 15;
    public bool AutoDeleteAuditEnabled { get; set; } = true;
    public int AuditRetentionMonths { get; set; } = 1;

    // Read by ThemeApplier at startup (desktop only - Android doesn't wire
    // IShopContextService, so the mobile app always renders Light,
    // unchanged from before this setting existed).
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    public UiDensity UiDensity { get; set; } = UiDensity.Comfortable;
    // Empty means follow this device's OS culture; otherwise stores a BCP-47
    // culture name used for app-wide number/date formatting.
    public string DisplayCulture { get; set; } = string.Empty;
    public bool PosSoundsEnabled { get; set; }
    public bool ShowPosNotifications { get; set; } = true;

    public string ServerBindAddress { get; set; } = "0.0.0.0";
    public int ServerPort { get; set; } = 5177;
}
