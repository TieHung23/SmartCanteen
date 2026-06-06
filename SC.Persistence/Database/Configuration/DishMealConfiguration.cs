using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Dish.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class DishMealConfiguration : IEntityTypeConfiguration<DishMeal>
{
    public void Configure(EntityTypeBuilder<DishMeal> builder)
    {
        builder.HasKey(x => new { x.DishId, x.MealId });

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasDefaultValue(1);
    }
}
