using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Domain.Dish.AggregateRoot;

public class Dish : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Dish()
    {
    }

    public required string Name { get; set; }
    public required string Description { get; set; }
    public Money Price { get; set; } = Money.Create(0);
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid MealId { get; set; }
    public Guid CategoryId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Dish Create(
        string name,
        string description,
        Money price,
        int stockQuantity,
        Guid mealId,
        Guid categoryId,
        Guid createdBy)
    {
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");

        return new Dish
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = stockQuantity,
            MealId = mealId,
            CategoryId = categoryId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void UpdateStock(int stockQuantity, Guid updatedBy)
    {
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");

        StockQuantity = stockQuantity;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Update(
        string name,
        string description,
        Money price,
        int stockQuantity,
        Guid mealId,
        Guid categoryId,
        bool isActive,
        Guid updatedBy)
    {
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        MealId = mealId;
        CategoryId = categoryId;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void MarkInactive(Guid updatedBy)
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(Guid updatedBy)
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
