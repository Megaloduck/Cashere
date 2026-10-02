using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cashere.Data.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.Property(e => e.ActorName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(20);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.EntityId).HasMaxLength(64);
        builder.Property(e => e.Summary).IsRequired().HasMaxLength(500);
        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => e.ActorCashierId);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Name).IsUnique();

        // SetNull rather than Restrict: deleting a tax rate that's still
        // assigned to categories shouldn't block the delete - those
        // categories simply fall back to the shop-wide default rate, same
        // "orphaned reference just goes to the fallback" spirit as
        // ProductConfiguration's own Category FK.
        builder.HasOne(c => c.TaxRate)
            .WithMany(t => t.Categories)
            .HasForeignKey(c => c.TaxRateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
    }
}

public class CashierConfiguration : IEntityTypeConfiguration<Cashier>
{
    public void Configure(EntityTypeBuilder<Cashier> builder)
    {
        builder.Property(c => c.Username).IsRequired().HasMaxLength(64);
        builder.Property(c => c.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.Username).IsUnique();
    }
}

public class ReceiptAdminConfiguration : IEntityTypeConfiguration<ReceiptAdmin>
{
    public void Configure(EntityTypeBuilder<ReceiptAdmin> builder)
    {
        builder.Property(s => s.ShopName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(10);
        builder.Property(s => s.TaxRatePercent).HasPrecision(5, 2);
        builder.Property(s => s.ServerBindAddress).IsRequired().HasMaxLength(64);
        builder.Property(s => s.OutOfStockBehavior).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.ThemeMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.UiDensity).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.DisplayCulture).IsRequired().HasMaxLength(32);
        builder.Property(s => s.RoundingMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.RoundingIncrement).HasPrecision(18, 2);
        builder.Property(s => s.SkuPrefix).IsRequired().HasMaxLength(12);
        builder.Property(s => s.InternalBarcodePrefix).IsRequired().HasMaxLength(2);
        builder.Property(s => s.SaleNumberPrefix).IsRequired().HasMaxLength(10);
        builder.Property(s => s.ScheduledBackupTime).IsRequired().HasMaxLength(5);
        builder.Property(s => s.OrderTypes).IsRequired().HasMaxLength(338);
    }
}
