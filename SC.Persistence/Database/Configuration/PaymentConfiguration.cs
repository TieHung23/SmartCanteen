using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Payment.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GatewayOrderId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.GatewayTransactionId)
            .HasMaxLength(200);

        builder.Property(x => x.AmountVnd)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.ConvertedPoints)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Method)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.GatewayOrderId).IsUnique();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.HasIndex(x => x.GatewayTransactionId)
            .IsUnique()
            .HasFilter("\"GatewayTransactionId\" IS NOT NULL");

        builder.Ignore(x => x.DomainEvents);
    }
}
