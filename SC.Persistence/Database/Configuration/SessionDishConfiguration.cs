using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Dish.AggregateRoot;

namespace SC.Persistence.Database.Configuration;

public class SessionDishConfiguration : IEntityTypeConfiguration<SessionDish>
{
    public void Configure(EntityTypeBuilder<SessionDish> builder)
    {
        builder.HasKey(x => new { x.DishId, x.SessionId });

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasDefaultValue(1);
    }
}
