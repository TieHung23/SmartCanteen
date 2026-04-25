namespace SC.Domain.Domain.Meal.ValueObject;

public class MealSettings : Abstraction.Aggregates.ValueObject
{
    public Guid CategoryId { get; set; }
    public Category.Category? Category { get; set; }
    
    public int Quantity { get; init; }
    public bool IsDeleted { get; init; }
    
    public Guid MealId { get; set; }
    public Meal.AggregateRoot.Meal? Meal { get; set; }
    
    private MealSettings() { }

    public static MealSettings Create(Category.Category category, int quantity, Meal.AggregateRoot.Meal meal)
    {
        if ( quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        return new MealSettings
        {
            Category = category,
            Quantity = quantity,
            IsDeleted = false,
            Meal = meal
        };
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CategoryId;
        yield return Quantity;
        yield return IsDeleted;
        yield return MealId;
    }
}