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

        builder.OwnsMany(x => x.OrderItems, orderItem =>
        {
            orderItem.ToTable("OrderItems");

            orderItem.WithOwner().HasForeignKey(x => x.OrderId);

            orderItem.HasKey(x => new { x.OrderId, x.ProductId });

            orderItem.Property(x => x.ProductId)
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

            orderItem.Ignore(x => x.Product);
            orderItem.Ignore(x => x.Order);
        });

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}