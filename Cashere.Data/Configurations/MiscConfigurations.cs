using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;  

namespace Cashere.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Name).IsUnique();
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

public class ShopSettingsConfiguration : IEntityTypeConfiguration<ShopSettings>
{
    public void Configure(EntityTypeBuilder<ShopSettings> builder)
    {
        builder.Property(s => s.ShopName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(10);
        builder.Property(s => s.TaxRatePercent).HasPrecision(5, 2);
        builder.Property(s => s.ServerBindAddress).IsRequired().HasMaxLength(64);
    }
}