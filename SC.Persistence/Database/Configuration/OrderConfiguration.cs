using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.ValueObject;

namespace SC.Persistence.Database.Configuration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Meal)
            .WithMany()
            .HasForeignKey(x => x.MealId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.OwnsMany(x => x.OrderItems, orderItem =>
        {
            orderItem.ToTable("OrderItems");

            orderItem.WithOwner().HasForeignKey(x => x.OrderId);

            orderItem.HasKey(x => new { x.OrderId, x.DishesId });

            orderItem.Property(x => x.DishesId)
                .IsRequired();

            orderItem.Property(x => x.Quantity)
                .IsRequired();

            orderItem.OwnsOne(x => x.UnitPrice, price =>
            {
                price.Property(x => x.Amount)
                    .HasColumnName("UnitPriceAmount")
                    .HasPrecision(18, 2);

                price.Property(x => x.Currency)
                    .HasColumnName("UnitPriceCurrency")
                    .HasMaxLength(10);
            });

            orderItem.Ignore(x => x.Dishes);
            orderItem.Ignore(x => x.Order);
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}