using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Meal.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.ToTable("Meals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.IsActive).IsRequired();

        builder.OwnsOne(x => x.Money, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("MoneyAmount")
                .HasPrecision(18, 2);

            money.Property(x => x.Currency)
                .HasColumnName("MoneyCurrency")
                .HasMaxLength(10);
        });

        builder.OwnsMany(x => x.MealSettingsList, mealSettings =>
        {
            mealSettings.ToTable("MealSettings");

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

            mealSettings.Ignore(x => x.Meal);
            mealSettings.Ignore(x => x.Category);
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}