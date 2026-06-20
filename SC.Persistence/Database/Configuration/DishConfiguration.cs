using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Dish.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.ImgUrl).HasMaxLength(2048);
        builder.Property(x => x.CategoryId).IsRequired();
        builder.HasIndex(x => x.CategoryId);

        builder.OwnsOne(x => x.Price, price =>
        {
            price.Property(x => x.Amount).HasPrecision(18, 2);
            price.Property(x => x.Currency).HasMaxLength(10);
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
