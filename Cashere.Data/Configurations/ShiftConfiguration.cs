using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cashere.Data.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.Property(s => s.StartingCash).HasPrecision(18, 2);
        builder.Property(s => s.ExpectedCashAtClose).HasPrecision(18, 2);
        builder.Property(s => s.ActualCashAtClose).HasPrecision(18, 2);
        builder.Property(s => s.DiscrepancyAmount).HasPrecision(18, 2);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.OpenedAt);

        builder.HasOne(s => s.Cashier)
            .WithMany(c => c.Shifts)
            .HasForeignKey(s => s.CashierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}