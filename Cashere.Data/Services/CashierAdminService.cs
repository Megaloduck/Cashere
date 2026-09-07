using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class CashierAdminService : ICashierAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public CashierAdminService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Cashier>> GetAllCashiersAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Cashiers.OrderBy(c => c.DisplayName).AsNoTracking().ToListAsync();
    }

    public async Task<Cashier> CreateCashierAsync(CashierInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var username = input.Username.Trim();
        if (await db.Cashiers.AnyAsync(c => c.Username == username))
        {
            throw new AdminValidationException($"Username '{username}' is already taken.");
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new AdminValidationException("A password is required for a new cashier.");
        }

        var cashier = new Cashier
        {
            Username = username,
            // Placeholder only, same as SeedData - there's no real hashing until
            // the login/authentication phase. Replace before going live.
            PasswordHash = input.Password,
            DisplayName = input.DisplayName.Trim(),
            Role = input.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Cashiers.Add(cashier);
        await db.SaveChangesAsync();
        return cashier;
    }

    public async Task UpdateCashierAsync(int cashierId, CashierInput input)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var cashier = await db.Cashiers.FirstOrDefaultAsync(c => c.Id == cashierId)
            ?? throw new AdminValidationException($"Cashier {cashierId} was not found.");

        var username = input.Username.Trim();
        if (await db.Cashiers.AnyAsync(c => c.Username == username && c.Id != cashierId))
        {
            throw new AdminValidationException($"Username '{username}' is already taken.");
        }

        cashier.Username = username;
        cashier.DisplayName = input.DisplayName.Trim();
        cashier.Role = input.Role;

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            cashier.PasswordHash = input.Password;
        }

        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int cashierId, bool isActive)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var cashier = await db.Cashiers.FirstOrDefaultAsync(c => c.Id == cashierId)
            ?? throw new AdminValidationException($"Cashier {cashierId} was not found.");

        cashier.IsActive = isActive;
        await db.SaveChangesAsync();
    }
}