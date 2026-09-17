using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cashere.Data.Configurations;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.Property(v => v.Code).IsRequired().HasMaxLength(20);
        builder.Property(v => v.DiscountType).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.DiscountValue).HasPrecision(18, 2);
        builder.Property(v => v.Notes).HasMaxLength(500);

        builder.HasIndex(v => v.Code).IsUnique();
    }
}

public class VoucherRedemptionConfiguration : IEntityTypeConfiguration<VoucherRedemption>
{
    public void Configure(EntityTypeBuilder<VoucherRedemption> builder)
    {
        builder.Property(r => r.DiscountAmount).HasPrecision(18, 2);

        builder.HasIndex(r => r.RedeemedAt);

        // Restrict, not Cascade - deleting a voucher that's already been
        // redeemed would silently erase the discount reason on real sales.
        // DeleteVoucherAsync below catches this and tells the admin to
        // deactivate instead, same fallback SupplierAdminService uses.
        builder.HasOne(r => r.Voucher)
            .WithMany(v => v.Redemptions)
            .HasForeignKey(r => r.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Sale)
            .WithMany(s => s.VoucherRedemptions)
            .HasForeignKey(r => r.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}