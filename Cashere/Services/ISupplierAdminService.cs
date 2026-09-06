using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public record SupplierInput(string Name, string? ContactPerson, string? Phone, string? Address, string? Notes);

public interface ISupplierAdminService
{
    Task<List<Supplier>> GetAllSuppliersAsync();
    Task<Supplier> CreateSupplierAsync(SupplierInput input);
    Task UpdateSupplierAsync(int supplierId, SupplierInput input);
    Task DeleteSupplierAsync(int supplierId);
}