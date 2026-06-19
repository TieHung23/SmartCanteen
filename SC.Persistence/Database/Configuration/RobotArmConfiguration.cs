using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.RobotArm.Entity;

namespace SC.Persistence.Database.Configuration;

public class RobotArmConfiguration : IEntityTypeConfiguration<RobotArm>
{
    public void Configure(EntityTypeBuilder<RobotArm> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(100);

        builder.Property(x => x.IpAddress)
            .IsRequired()
            .HasMaxLength(45);   // đủ cho IPv6

        builder.Property(x => x.StationIndex).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.LastHeartbeatUtc);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
