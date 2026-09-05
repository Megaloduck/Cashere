using System.Threading.Tasks;
using Cashere.Models;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(CashereDbContext db)
    {
        if (!await db.ShopSettings.AnyAsync())
        {
            db.ShopSettings.Add(new ShopSettings
            {
                ShopName = "My Shop",
                Currency = "IDR",
                TaxRatePercent = 0
            });
        }

        if (!await db.Cashiers.AnyAsync())
        {
            db.Cashiers.Add(new Cashier
            {
                Username = "admin",
                // Placeholder only - there's no login screen or password
                // hashing yet, replace this before going live.
                PasswordHash = "admin",
                DisplayName = "Admin",
                Role = UserRole.Owner,
                IsActive = true
            });
        }

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.Add(new Category { Name = "General" });
        }

        await db.SaveChangesAsync();
    }
}
