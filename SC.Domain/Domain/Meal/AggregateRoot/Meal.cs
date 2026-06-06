using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.Domain.Meal.Entity;

// ReSharper disable All

namespace SC.Domain.Domain.Meal.AggregateRoot;

public class Meal : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Meal()
    {
    }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<DishMeal> DishMeals { get; set; } = new List<DishMeal>();
    public ICollection<MealTemplate> MealTemplates { get; set; } = new List<MealTemplate>();
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Meal Create(string name, string description, DateTimeOffset availableFrom, DateTimeOffset availableTo, DateTimeOffset availableForOrder, Guid createdBy)
    {
        return new Meal
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            AvailableFrom = availableFrom,
            AvailableTo = availableTo,
            AvailableForOrder = availableForOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string description, DateTimeOffset availableFrom, DateTimeOffset availableTo, DateTimeOffset availableForOrder, bool isActive, Guid updatedBy)
    {
        Name = name;
        Description = description;
        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
        AvailableForOrder = availableForOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void AddDishMeal(DishMeal dishMeal)
    {
        DishMeals.Add(dishMeal);
    }

    public void RemoveDishMeal(DishMeal dishMeal)
    {
        DishMeals.Remove(dishMeal);
    }

    public void AddMealTemplate(MealTemplate mealTemplate)
    {
        MealTemplates.Add(mealTemplate);
    }

    public void RemoveMealTemplate(MealTemplate mealTemplate)
    {
        MealTemplates.Remove(mealTemplate);
    }

    public void ClearMealTemplates()
    {
        MealTemplates.Clear();
    }
}