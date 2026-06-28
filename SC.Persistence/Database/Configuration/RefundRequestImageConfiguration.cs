using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Refund.Entity;

namespace SC.Persistence.Database.Configuration;

public class RefundRequestImageConfiguration : IEntityTypeConfiguration<RefundRequestImage>
{
    public void Configure(EntityTypeBuilder<RefundRequestImage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RefundRequestId)
            .IsRequired();

        builder.HasIndex(x => x.RefundRequestId);

        builder.Property(x => x.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.UploadedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();
    }
}
