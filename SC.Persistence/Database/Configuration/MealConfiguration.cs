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

        builder.HasMany(x => x.DishMeals)
            .WithOne(x => x.Meal)
            .HasForeignKey(x => x.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MealTemplates)
            .WithOne(x => x.Meal)
            .HasForeignKey(x => x.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}