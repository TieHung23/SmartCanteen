using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.Dish;

namespace SC.Persistence.Database.Configuration;

public class SessionDishConfiguration : IEntityTypeConfiguration<SessionDish>
{
    public void Configure(EntityTypeBuilder<SessionDish> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DishId).IsRequired();
        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.Quantity).IsRequired().HasDefaultValue(1);

        builder.HasIndex(x => new { x.SessionId, x.DishId }).IsUnique();
    }
}
