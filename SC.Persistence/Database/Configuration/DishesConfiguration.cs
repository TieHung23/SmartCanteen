using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Dishes.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class DishesConfiguration : IEntityTypeConfiguration<Dishes>
{
    public void Configure(EntityTypeBuilder<Dishes> builder)
    {
        builder.ToTable("Dishes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.StockQuantity)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.OwnsOne(x => x.Price, price =>
        {
            price.Property(x => x.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 2);

            price.Property(x => x.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(10);
        });

        builder.HasOne(x => x.Meal)
            .WithMany()
            .HasForeignKey(x => x.MealId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}