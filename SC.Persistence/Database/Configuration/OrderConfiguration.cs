using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Order.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MealId).IsRequired();
        builder.HasIndex(x => x.MealId);

        builder.Property(x => x.PaymentId);
        builder.HasIndex(x => x.PaymentId);

        builder.Property(x => x.Status).IsRequired();

        builder.OwnsMany(x => x.OrderItems, orderItem =>
        {

            orderItem.WithOwner().HasForeignKey(x => x.OrderId);

            orderItem.HasKey(x => new { x.OrderId, x.DishId });

            orderItem.Property(x => x.DishId)
                .IsRequired();

            orderItem.Property(x => x.Quantity)
                .IsRequired();

            orderItem.OwnsOne(x => x.UnitPrice, price =>
            {
                price.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                price.Property(x => x.Currency)
                    .HasMaxLength(10);
            });
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}