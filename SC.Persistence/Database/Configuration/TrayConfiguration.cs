using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Tray.Entity;

namespace SC.Persistence.Database.Configuration;

public class TrayConfiguration : IEntityTypeConfiguration<Tray>
{
    public void Configure(EntityTypeBuilder<Tray> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(64);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CurrentOrderId);
        builder.HasIndex(x => x.CurrentOrderId);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
