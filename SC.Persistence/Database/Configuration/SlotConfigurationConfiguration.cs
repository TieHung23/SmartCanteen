using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.SlotConfiguration.Entity;

namespace SC.Persistence.Database.Configuration;

public class SlotConfigurationConfiguration : IEntityTypeConfiguration<SlotConfiguration>
{
    public void Configure(EntityTypeBuilder<SlotConfiguration> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.MealId).IsRequired();
        builder.Property(x => x.DishId).IsRequired();
        builder.Property(x => x.RobotArmId);

        builder.Property(x => x.LaneCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Capacity).IsRequired();

        // 1 lane trong 1 phiên là duy nhất
        builder.HasIndex(x => new { x.MealId, x.LaneCode }).IsUnique();
        builder.HasIndex(x => x.DishId);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
