namespace Cashere.Sync.Dtos;

// Read-only projection of a Product used for catalog sync to mobile.
// Deliberately excludes CostPrice - mobile doesn't need cost data. QrIdentity
// is the Cashere-specific QR payload; scanning it adds this product to the
// shared cart just like scanning its retail barcode.
public record ProductDto(
    int Id,
    string Sku,
    string? Barcode,
    string QrIdentity,
    string Name,
    string Unit,
    decimal SellingPrice,
    int StockQuantity,
    string? CategoryName,
    bool IsActive,
    bool HasPhoto);
