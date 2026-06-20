using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SC.Domain.Domain.ShelfStock.Entity;

namespace SC.Persistence.Database.Configuration;

public class ShelfStockConfiguration : IEntityTypeConfiguration<ShelfStock>
{
    public void Configure(EntityTypeBuilder<ShelfStock> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.DishId).IsRequired();
        builder.Property(x => x.SlotConfigurationId);
        builder.Property(x => x.Quantity).IsRequired();

        // Tồn kho 1 món trong 1 phiên là duy nhất
        builder.HasIndex(x => new { x.SessionId, x.DishId }).IsUnique();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
    }
}
