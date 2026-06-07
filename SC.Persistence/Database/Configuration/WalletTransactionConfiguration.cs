using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.WalletTransaction.Entity;

namespace SC.Persistence.Database.Configuration;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.BalanceBefore)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.BalanceAfter)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.TransactionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.PaymentId);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
