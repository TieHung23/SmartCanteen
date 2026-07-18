using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.ServingJob.Entity;

namespace SC.Persistence.Database.Configuration;

public class ServingJobConfiguration : IEntityTypeConfiguration<ServingJob>
{
    public void Configure(EntityTypeBuilder<ServingJob> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.HasIndex(x => x.OrderId);

        builder.Property(x => x.TrayId);
        builder.HasIndex(x => x.TrayId);   // query ngược "khay này thuộc job/đơn nào" (thay Tray.CurrentOrderId)
        builder.Property(x => x.PickupSlotId);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
