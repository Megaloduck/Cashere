using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cashere.Data;

public class CashereDbContext : DbContext
{
    public CashereDbContext(DbContextOptions<CashereDbContext> options) : base(options)
    {
    }

    public DbSet<ReceiptAdmin> ReceiptAdmin => Set<ReceiptAdmin>();
    public DbSet<Cashier> Cashiers => Set<Cashier>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<RefundLineItem> RefundLineItems => Set<RefundLineItem>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherRedemption> VoucherRedemptions => Set<VoucherRedemption>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CashereDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddAuditEvents();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AddAuditEvents();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AddAuditEvents()
    {
        var auditableTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(Cashier), nameof(Category), nameof(Customer), nameof(InventoryMovement), nameof(Product),
            nameof(Purchase), nameof(ReceiptAdmin), nameof(Refund), nameof(Sale), nameof(Shift),
            nameof(Supplier), nameof(TaxRate), nameof(Voucher)
        };
        var changes = ChangeTracker.Entries()
            .Where(entry => entry.Entity is not AuditEvent &&
                auditableTypes.Contains(entry.Metadata.ClrType.Name) &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (changes.Count == 0) return;

        var (cashierId, displayName) = CurrentCashierContext.Snapshot();
        foreach (var entry in changes)
        {
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => "Changed"
            };
            var key = entry.Properties.FirstOrDefault(property => property.Metadata.IsPrimaryKey());
            var keyValue = entry.State == EntityState.Deleted ? key?.OriginalValue : key?.CurrentValue;
            var changedFields = entry.State == EntityState.Modified
                ? string.Join(", ", entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name))
                : string.Empty;
            AuditEvents.Add(new AuditEvent
            {
                OccurredAtUtc = DateTime.UtcNow,
                ActorCashierId = cashierId,
                ActorName = displayName,
                Action = action,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = keyValue is null || Convert.ToInt64(keyValue) == 0 ? null : Convert.ToString(keyValue, System.Globalization.CultureInfo.InvariantCulture),
                Summary = changedFields.Length == 0 ? $"{action} {entry.Metadata.ClrType.Name}" : $"{action} {entry.Metadata.ClrType.Name}: {changedFields}"
            });
        }
    }

    public static string GetDefaultDbPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cashere");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "cashere.db");
    }
}
