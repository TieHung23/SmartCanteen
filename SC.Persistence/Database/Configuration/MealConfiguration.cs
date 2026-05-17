using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Meal.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.IsActive).IsRequired();

        builder.OwnsOne(x => x.Price, price =>
        {
            price.Property(x => x.Amount)
                .HasPrecision(18, 2);

            price.Property(x => x.Currency)
                .HasMaxLength(10);
        });

        builder.OwnsMany(x => x.MealSettingsList, mealSettings =>
        {

            mealSettings.WithOwner().HasForeignKey(x => x.MealId);

            mealSettings.HasKey(x => new { x.MealId, x.CategoryId });

            mealSettings.Property(x => x.CategoryId)
                .IsRequired();

            mealSettings.Property(x => x.MealId)
                .IsRequired();

            mealSettings.Property(x => x.Quantity)
                .IsRequired();

            mealSettings.Property(x => x.IsDeleted)
                .IsRequired();
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}