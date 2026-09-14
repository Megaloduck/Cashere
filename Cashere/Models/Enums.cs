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

// Read by SaleService at checkout, only when TrackInventory is on - see
// ReceiptAdmin.OutOfStockBehavior.
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