using SC.Domain.Abstraction.Entities;
using CategoryAggregate = SC.Domain.Domain.Category.Category;
using MealAggregate = SC.Domain.Domain.Meal.AggregateRoot.Meal;
using SC.Domain.Domain.User.ValueObject;

namespace SC.Domain.Domain.Dishes.AggregateRoot;

public class Dishes : Entity<Guid>, IAuditableEntity<Guid>
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public Money Price { get; set; } = Money.Create(0);
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid MealId { get; set; }
    public MealAggregate? Meal { get; set; }

    public Guid CategoryId { get; set; }
    public CategoryAggregate? Category { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    private Dishes() { }

    public static Dishes Create(
        string name,
        string description,
        Money price,
        int stockQuantity,
        MealAggregate meal,
        CategoryAggregate category,
        Guid createdBy)
    {
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");

        return new Dishes
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = stockQuantity,
            Meal = meal,
            MealId = meal.Id,
            Category = category,
            CategoryId = category.Id,
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

    public void MarkInactive(Guid updatedBy)
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}