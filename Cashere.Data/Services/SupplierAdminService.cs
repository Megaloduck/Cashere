using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class SupplierAdminService : ISupplierAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public SupplierAdminService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Supplier>> GetAllSuppliersAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Suppliers.OrderBy(s => s.Name).AsNoTracking().ToListAsync();
    }

    public async Task<Supplier> CreateSupplierAsync(SupplierInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var supplier = new Supplier
        {
            Name = input.Name.Trim(),
            ContactPerson = Normalize(input.ContactPerson),
            Phone = Normalize(input.Phone),
            Address = Normalize(input.Address),
            Notes = Normalize(input.Notes)
        };

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier;
    }

    public async Task UpdateSupplierAsync(int supplierId, SupplierInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId)
            ?? throw new AdminValidationException($"Supplier {supplierId} was not found.");

        supplier.Name = input.Name.Trim();
        supplier.ContactPerson = Normalize(input.ContactPerson);
        supplier.Phone = Normalize(input.Phone);
        supplier.Address = Normalize(input.Address);
        supplier.Notes = Normalize(input.Notes);

        await db.SaveChangesAsync();
    }

    public async Task DeleteSupplierAsync(int supplierId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (supplier is null) return;

        try
        {
            db.Suppliers.Remove(supplier);
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Purchases reference suppliers with OnDelete(Restrict) - once
            // purchase recording exists, a supplier with history can't be
            // deleted, so this surfaces as a friendly message instead of an
            // unhandled exception.
            throw new AdminValidationException(
                $"'{supplier.Name}' can't be deleted because it has purchase history.");
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}