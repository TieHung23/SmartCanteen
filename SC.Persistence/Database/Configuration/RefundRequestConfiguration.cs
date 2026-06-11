using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.User;
using SC.Domain.Domain.WalletTransaction.Entity;

namespace SC.Persistence.Database.Configuration;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.OrderId).IsRequired();
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.OrderId)
            .IsUnique()
            .HasDatabaseName("IX_RefundRequests_OrderId_Active")
            .HasFilter("\"IsDeleted\" = FALSE AND \"Status\" IN (1, 2)");

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.PolicyCode)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.PolicyNameSnapshot)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.RefundPercentSnapshot)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(x => x.OrderAmountSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.RefundAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasIndex(x => x.Status);

        builder.Property(x => x.ReviewedBy);
        builder.Property(x => x.ReviewedAtUtc);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.WalletTransactionId);
        builder.HasIndex(x => x.WalletTransactionId)
            .IsUnique()
            .HasFilter("\"WalletTransactionId\" IS NOT NULL");

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WalletTransaction>()
            .WithMany()
            .HasForeignKey(x => x.WalletTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Images)
            .WithOne()
            .HasForeignKey(x => x.RefundRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
