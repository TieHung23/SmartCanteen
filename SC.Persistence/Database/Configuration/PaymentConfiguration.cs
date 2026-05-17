using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Payment.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GatewayTransactionId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Method)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.OwnsOne(x => x.BalanceSnapshot, snapshot =>
        {
            snapshot.Property(x => x.DeltaAmount)
                .HasPrecision(18, 2);

            snapshot.Property(x => x.BalanceBefore)
                .HasPrecision(18, 2);

            snapshot.Property(x => x.BalanceAfter)
                .HasPrecision(18, 2);
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}