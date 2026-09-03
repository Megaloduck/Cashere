using Cashere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cashere.Data.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.Property(m => m.MovementType).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.ReferenceType).HasMaxLength(30);

        builder.HasIndex(m => m.CreatedAt);

        builder.HasOne(m => m.Product)
            .WithMany(p => p.InventoryMovements)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
