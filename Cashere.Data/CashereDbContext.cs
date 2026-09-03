using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Collections.Generic;


namespace Cashere.Data;

public class CashereDbContext : DbContext
{
    public CashereDbContext(DbContextOptions<CashereDbContext> options) : base(options)
    {
    }

    public DbSet<ShopSettings> ShopSettings => Set<ShopSettings>();
    public DbSet<Cashier> Cashiers => Set<Cashier>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CashereDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // Windows: %LocalAppData%\Cashere\cashere.db (works the same way once Mac/Linux desktop targets are added).
    public static string GetDefaultDbPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cashere");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "cashere.db");
    }
}
