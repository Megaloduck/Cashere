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

public interface IProductAdminService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product> CreateProductAsync(ProductInput input);
    Task UpdateProductAsync(int productId, ProductInput input);
    Task SetActiveAsync(int productId, bool isActive);

    // Set by the server's photo-upload endpoint once a mobile-captured photo
    // has been saved to disk, or by desktop admin to manually clear a bad
    // photo. Deliberately separate from ProductInput/UpdateProductAsync -
    // the photo lifecycle (capture -> upload -> store) is independent of the
    // rest of the product form.
    Task SetPhotoPathAsync(int productId, string? photoPath);
}