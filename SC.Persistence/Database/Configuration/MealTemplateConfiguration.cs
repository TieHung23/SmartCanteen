using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Session.Entity;

namespace SC.Persistence.Database.Configuration;

public class MealTemplateConfiguration : IEntityTypeConfiguration<MealTemplate>
{
    public void Configure(EntityTypeBuilder<MealTemplate> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(x => x.Settings)
            .WithOne()
            .HasForeignKey(x => x.MealTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
