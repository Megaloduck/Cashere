using System.Threading.Tasks;
using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using Cashere.Data.Services;

namespace Cashere.Data;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(CashereDbContext db)
    {
        if (!await db.ReceiptAdmin.AnyAsync())
        {
            db.ReceiptAdmin.Add(new ReceiptAdmin
            {
                ShopName = "My Shop",
                Currency = "IDR",
                TaxRatePercent = 0,
                ServerBindAddress = "0.0.0.0",
                ServerPort = 5177
            });
        }

        if (!await db.Cashiers.AnyAsync())
        {
            // Same default admin/admin credentials as before - now stored as
            // a real PBKDF2 hash instead of the literal string "admin".
            // Instantiated directly rather than injected: seeding runs
            // before any DI/service wiring exists, at first-launch migration
            // time in Program.cs.
            var hasher = new Pbkdf2PasswordHasher();
            db.Cashiers.Add(new Cashier
            {
                Username = "admin",
                PasswordHash = hasher.Hash("admin"),
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