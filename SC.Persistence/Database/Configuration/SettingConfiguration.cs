using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Setting.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Value)
            .IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
