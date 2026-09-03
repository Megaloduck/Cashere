namespace Cashere.Sync.Dtos;

// Read-only projection of a Product used for catalog sync to mobile.
// Deliberately excludes CostPrice - mobile doesn't need cost data.
public record ProductDto(
    int Id,
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal SellingPrice,
    int StockQuantity,
    string? CategoryName,
    bool IsActive);
