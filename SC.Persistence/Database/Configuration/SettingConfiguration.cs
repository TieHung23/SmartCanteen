using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Setting.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.Description)
            .HasMaxLength(500);
            
        builder.Property(x => x.Group)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Scope)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Value)
            .IsRequired();

        builder.HasIndex(x => new { x.Group, x.Scope, x.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Ignore(x => x.DomainEvents);
    }
}
