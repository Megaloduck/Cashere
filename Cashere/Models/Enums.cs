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
