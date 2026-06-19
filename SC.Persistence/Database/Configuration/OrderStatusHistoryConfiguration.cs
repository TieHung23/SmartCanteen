using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.OrderStatusHistory.Entity;

namespace SC.Persistence.Database.Configuration;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.OrderId).IsRequired();
        builder.HasIndex(x => x.OrderId);

        builder.Property(x => x.FromStatus)
            .HasConversion<int>();

        builder.Property(x => x.ToStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ReasonCode)
            .HasMaxLength(100);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
