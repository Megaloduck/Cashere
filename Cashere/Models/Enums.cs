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