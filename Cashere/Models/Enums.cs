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
    Refunded
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

// Read by ThemeApplier at app startup and whenever Settings -> Preferences
// saves. System maps to Avalonia's ThemeVariant.Default, which follows the
// OS theme automatically (including live OS theme changes) with no extra
// wiring needed here.
public enum AppThemeMode
{
    Light,
    Dark,
    System
}