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

    // Business Info's Timezone picker. Purely informational/reference (what
    // timezone the shop is physically in, e.g. for printed info) - it does
    // NOT affect how any timestamp is displayed. Stored as a TimezoneOption
    // label (e.g. "Jakarta (GMT+7)") from TimezonePresets.FixedOffsets.
    // Actual display always follows this device's own local clock; see
    // ClockPreferenceService.
    public string? Timezone { get; set; }

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

    public bool CashEnabled { get; set; } = true;
    public bool QrisEnabled { get; set; } = true;
    public bool EdcEnabled { get; set; } = true;
    public string? QrisAccountInfo { get; set; }
    public string? EdcAccountInfo { get; set; }
    public bool RequireConfirmationForNonCash { get; set; }

    public string? PrinterName { get; set; }
    public int PrinterPaperWidthMm { get; set; } = 80;

    public bool RequireCustomerBeforeCheckout { get; set; }
    public bool AutoPrintReceiptAfterPayment { get; set; }

    // Settings -> Security. Enforced by AutoLockService, configured from
    // RootViewModel right after login and re-applied live on Save - same
    // "takes effect immediately" feel as ThemeMode below.
    public bool AutoLockEnabled { get; set; }
    public int AutoLockTimeoutMinutes { get; set; } = 15;

    // Read by ThemeApplier at startup (desktop only - Android doesn't wire
    // IShopContextService, so the mobile app always renders Light,
    // unchanged from before this setting existed).
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;

    public string ServerBindAddress { get; set; } = "0.0.0.0";
    public int ServerPort { get; set; } = 5177;
}