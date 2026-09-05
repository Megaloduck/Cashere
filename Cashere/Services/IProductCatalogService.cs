using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

// Deliberately EF-free (mirrors the Cashere.Sync boundary): the shared Cashere
// project only knows about this contract, never about CashereDbContext. The
// concrete, EF-backed implementation lives in Cashere.Data and is wired up by
// Cashere.Desktop at startup.
public interface IProductCatalogService
{
    Task<List<Product>> GetActiveProductsAsync();
    Task<List<Category>> GetCategoriesAsync();
    Task<Product?> FindByBarcodeAsync(string barcode);
}

