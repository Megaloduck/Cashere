using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public record ProductInput(
    string Sku,
    string? Barcode,
    string Name,
    int? CategoryId,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int StockQuantity,
    int LowStockThreshold);

// Admin-side CRUD for the product catalog - deliberately separate from
// IProductCatalogService, which is the POS-facing, active-only, read-only
// view of the same table. Implemented in Cashere.Data with EF Core, same
// split as every other service pair in this project.
public interface IProductAdminService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product> CreateProductAsync(ProductInput input);
    Task UpdateProductAsync(int productId, ProductInput input);
    Task SetActiveAsync(int productId, bool isActive);
}