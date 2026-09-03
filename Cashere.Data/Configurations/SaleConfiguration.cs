using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cashere.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(32);
        builder.Property(s => s.Subtotal).HasPrecision(18, 2);
        builder.Property(s => s.DiscountAmount).HasPrecision(18, 2);
        builder.Property(s => s.TaxAmount).HasPrecision(18, 2);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.SaleNumber).IsUnique();
        builder.HasIndex(s => s.SaleDate);

        builder.HasOne(s => s.Cashier)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.UnitCostAtSale).HasPrecision(18, 2);
        builder.Property(i => i.Subtotal).HasPrecision(18, 2);

        builder.HasOne(i => i.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
