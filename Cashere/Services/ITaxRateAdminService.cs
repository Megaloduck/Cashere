using System.Collections.Generic;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public record TaxRateInput(string Name, decimal RatePercent);

// Mirrors ICategoryAdminService's shape almost exactly - same CRUD
// pattern used throughout Cashere.Data.Services (Supplier, Voucher,
// Category admin services all look like this).
public interface ITaxRateAdminService
{
    Task<List<TaxRate>> GetAllTaxRatesAsync();
    Task<TaxRate> CreateTaxRateAsync(TaxRateInput input);
    Task UpdateTaxRateAsync(int taxRateId, TaxRateInput input);
    Task DeleteTaxRateAsync(int taxRateId);

    // Read by the Business Info screen's category-assignment grid.
    Task<List<Category>> GetCategoriesWithTaxRatesAsync();
    Task AssignCategoryTaxRateAsync(int categoryId, int? taxRateId);
}
