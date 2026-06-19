using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.RobotEventLog.Entity;

namespace SC.Persistence.Database.Configuration;

public class RobotEventLogConfiguration : IEntityTypeConfiguration<RobotEventLog>
{
    public void Configure(EntityTypeBuilder<RobotEventLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.RobotArmId);
        builder.Property(x => x.ServingJobId);
        builder.HasIndex(x => x.ServingJobId);
        builder.Property(x => x.OrderId);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Message)
            .HasMaxLength(1000);

        builder.Property(x => x.PayloadJson);

        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.HasIndex(x => x.OccurredAtUtc);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
