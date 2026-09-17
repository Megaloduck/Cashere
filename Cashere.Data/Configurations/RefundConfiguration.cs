using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cashere.Data.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
        builder.Property(r => r.Reason).HasMaxLength(500);

        builder.HasIndex(r => r.RefundDate);

        builder.HasOne(r => r.Sale)
            .WithMany(s => s.Refunds)
            .HasForeignKey(r => r.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ProcessedByCashier)
            .WithMany()
            .HasForeignKey(r => r.ProcessedByCashierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefundLineItemConfiguration : IEntityTypeConfiguration<RefundLineItem>
{
    public void Configure(EntityTypeBuilder<RefundLineItem> builder)
    {
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Subtotal).HasPrecision(18, 2);

        builder.HasOne(i => i.Refund)
            .WithMany(r => r.Lines)
            .HasForeignKey(i => i.RefundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.SaleItem)
            .WithMany(si => si.RefundLineItems)
            .HasForeignKey(i => i.SaleItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}