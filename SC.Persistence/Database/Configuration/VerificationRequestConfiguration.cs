using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.User;
using SC.Domain.Domain.Verification.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class VerificationRequestConfiguration : IEntityTypeConfiguration<VerificationRequest>
{
    public void Configure(EntityTypeBuilder<VerificationRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasIndex(x => x.Status);

        builder.Property(x => x.SubmittedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.ReviewedAt);
        builder.Property(x => x.ReviewedBy);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(x => x.Documents, document =>
        {
            document.ToTable("VerificationDocuments");

            document.WithOwner().HasForeignKey(x => x.VerificationRequestId);

            document.HasKey(x => new { x.VerificationRequestId, x.Id });

            document.Property(x => x.DocumentType)
                .IsRequired()
                .HasConversion<int>();

            document.Property(x => x.CloudinaryUrl)
                .IsRequired()
                .HasMaxLength(1000);

            document.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(255);

            document.Property(x => x.FileSize)
                .IsRequired();

            document.Property(x => x.MimeType)
                .IsRequired()
                .HasMaxLength(100);

            document.Property(x => x.UploadedAt)
                .IsRequired();
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
