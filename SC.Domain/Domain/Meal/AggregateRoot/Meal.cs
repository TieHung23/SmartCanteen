using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Meal.ValueObject;
using SC.Domain.SharedKernel.ValueObjects;

// ReSharper disable All

namespace SC.Domain.Domain.Meal.AggregateRoot;

public class Meal : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Meal()
    {
    }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Money Price { get; set; } = Money.Create(0);
    public bool IsActive { get; set; } = true;
    public IList<MealSettings> MealSettingsList { get; set; } = new List<MealSettings>();
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Meal Create(string name, string description, Money price, DateTimeOffset availableFrom, DateTimeOffset availableTo, DateTimeOffset availableForOrder, Guid createdBy)
    {
        return new Meal
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            AvailableFrom = availableFrom,
            AvailableTo = availableTo,
            AvailableForOrder = availableForOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string description, Money price, DateTimeOffset availableFrom, DateTimeOffset availableTo, DateTimeOffset availableForOrder, bool isActive, Guid updatedBy)
    {
        Name = name;
        Description = description;
        Price = price;
        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
        AvailableForOrder = availableForOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void AddMealCategory(MealSettings mealSettings)
    {
        MealSettingsList.Add(mealSettings);
    }

    public void RemoveMealCategory(MealSettings mealSettings)
    {
        MealSettingsList.Remove(mealSettings);
    }

    public void ClearMealCategories()
    {
        MealSettingsList.Clear();
    }
}