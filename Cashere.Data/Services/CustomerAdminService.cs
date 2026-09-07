using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class CustomerAdminService : ICustomerAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public CustomerAdminService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Customers.OrderBy(c => c.Name).AsNoTracking().ToListAsync();
    }

    public async Task<Customer> CreateCustomerAsync(CustomerInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var customer = new Customer
        {
            Name = input.Name.Trim(),
            Phone = Normalize(input.Phone),
            Email = Normalize(input.Email),
            Address = Normalize(input.Address),
            Notes = Normalize(input.Notes),
            CreatedAt = DateTime.UtcNow
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateCustomerAsync(int customerId, CustomerInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == customerId)
            ?? throw new AdminValidationException($"Customer {customerId} was not found.");

        customer.Name = input.Name.Trim();
        customer.Phone = Normalize(input.Phone);
        customer.Email = Normalize(input.Email);
        customer.Address = Normalize(input.Address);
        customer.Notes = Normalize(input.Notes);

        await db.SaveChangesAsync();
    }

    public async Task DeleteCustomerAsync(int customerId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer is null) return;

        // Unlike suppliers, Sale->Customer is OnDelete(SetNull), so a customer
        // with purchase history can still be deleted - their past sales just
        // lose the customer reference instead of blocking the delete.
        db.Customers.Remove(customer);
        await db.SaveChangesAsync();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}