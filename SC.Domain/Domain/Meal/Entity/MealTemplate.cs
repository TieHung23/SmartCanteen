using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Meal.Entity;

public class MealTemplate : Entity<Guid>, IAuditableEntity<Guid>
{
    private MealTemplate()
    {
    }

    public required Guid MealId { get; set; }
    public required string Name { get; set; }

    public AggregateRoot.Meal Meal { get; set; } = null!;
    public ICollection<MealSettings> Settings { get; set; } = new List<MealSettings>();

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static MealTemplate Create(Guid mealId, string name)
    {
        return new MealTemplate
        {
            Id = Guid.NewGuid(),
            MealId = mealId,
            Name = name,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void AddSetting(Guid categoryId, int minQuantity, int maxQuantity, bool isRequired)
    {
        Settings.Add(MealSettings.Create(Id, categoryId, minQuantity, maxQuantity, isRequired));
    }

    public void ClearSettings()
    {
        Settings.Clear();
    }
}
