using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Persistence.Database.Logging;

namespace SC.Persistence.Database.Configuration;

public class ApplicationLogConfiguration : IEntityTypeConfiguration<ApplicationLog>
{
    public void Configure(EntityTypeBuilder<ApplicationLog> builder)
    {
        builder.ToTable("ApplicationLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Message)
            .IsRequired();

        builder.Property(x => x.MessageTemplate)
            .IsRequired();

        builder.Property(x => x.Level)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.TimeStamp)
            .IsRequired();

        builder.Property(x => x.Exception);

        builder.Property(x => x.Properties)
            .HasColumnType("jsonb");

        builder.Property(x => x.UserId)
            .HasMaxLength(128);

        builder.Property(x => x.RequestPath);

        builder.Property(x => x.HttpMethod)
            .HasMaxLength(16);
    }
}