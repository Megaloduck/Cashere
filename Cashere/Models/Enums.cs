namespace Cashere.Models;

public enum UserRole
{
    Owner,
    Manager,
    Cashier
}

public enum PaymentMethod
{
    Cash,
    Qris,
    Edc
}

public enum SaleStatus
{
    Completed,
    Voided,
    Refunded,
    // A refund covering some but not all lines/quantities - Completed and
    // PartiallyRefunded sales both still generated real net revenue, unlike
    // Voided/fully Refunded ones, so SalesReportService treats them as
    // "still counts" and nets out exactly what was returned.
    PartiallyRefunded
}

public enum InventoryMovementType
{
    PurchaseReceived,
    SaleDeducted,
    Adjustment,
    Return
}

public enum OutOfStockBehavior
{
    Block,
    AllowNegativeStock
}

public enum ShiftStatus
{
    Open,
    Closed
}

public enum AppThemeMode
{
    Light,
    Dark,
    System
}

public enum UiDensity
{
    Comfortable,
    Compact
}

// Settings -> Payments -> Rounding. Applied to Sale.TotalAmount only
// (after discount and tax), never to individual lines - see
// TaxCalculator.ApplyRounding, shared by CartViewModel's live preview and
// SaleService's authoritative total so the two can never disagree.
public enum RoundingMode
{
    // No rounding - TotalAmount is whatever discount/tax math produces.
    None,
    // Rounds to the nearest RoundingIncrement (e.g. nearest 100).
    Nearest,
    // Always rounds up to the next RoundingIncrement - the common choice
    // for cash businesses that never want to hand back more change than
    // they took in.
    Up
}

// ClockSource (SystemLocal/Utc) was removed: Cashere runs as a single
// local install, so every displayed timestamp now always follows this
// device's own local time zone with no configuration needed - see
// ClockPreferenceService.
