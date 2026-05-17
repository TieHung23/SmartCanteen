namespace SC.Domain.Domain.Meal.ValueObject;

public class MealSettings : Abstraction.Aggregates.ValueObject
{
    private MealSettings()
    {
    }

    public Guid CategoryId { get; set; }
    public int Quantity { get; init; }
    public bool IsDeleted { get; init; }

    public Guid MealId { get; set; }

    public static MealSettings Create(Guid categoryId, int quantity, Guid mealId)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        return new MealSettings
        {
            CategoryId = categoryId,
            Quantity = quantity,
            IsDeleted = false,
            MealId = mealId
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