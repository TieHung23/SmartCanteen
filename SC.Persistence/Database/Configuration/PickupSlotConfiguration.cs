using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.PickupSlot.Entity;

namespace SC.Persistence.Database.Configuration;

public class PickupSlotConfiguration : IEntityTypeConfiguration<PickupSlot>
{
    public void Configure(EntityTypeBuilder<PickupSlot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(64);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.OrderId);
        builder.HasIndex(x => x.OrderId);

        builder.Property(x => x.TrayId);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
