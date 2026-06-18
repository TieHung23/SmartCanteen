using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Session.Entity;

namespace SC.Persistence.Database.Configuration;

public class MealSettingsConfiguration : IEntityTypeConfiguration<MealSettings>
{
    public void Configure(EntityTypeBuilder<MealSettings> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.MealTemplateId).IsRequired();
        builder.Property(x => x.CategoryId).IsRequired();

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId);

        builder.Property(x => x.MinQuantity).IsRequired();
        builder.Property(x => x.MaxQuantity).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();

        builder.HasIndex(x => x.MealTemplateId);
        builder.HasIndex(x => x.CategoryId);
    }
}
