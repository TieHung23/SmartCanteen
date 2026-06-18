using SC.Domain.Abstraction.Entities;
using CategoryAggregate = SC.Domain.Domain.Category.AggregateRoot.Category;

namespace SC.Domain.Domain.Session.Entity;

public class MealSettings : Entity<Guid>, IAuditableEntity<Guid>
{
    private MealSettings()
    {
    }

    public required Guid MealTemplateId { get; set; }
    public required Guid CategoryId { get; set; }
    public CategoryAggregate Category { get; set; } = null!;
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public bool IsRequired { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static MealSettings Create(Guid mealTemplateId, Guid categoryId, int minQuantity, int maxQuantity, bool isRequired)
    {
        if (isRequired && minQuantity <= 0)
            throw new ArgumentException("MinQuantity must be greater than 0 when IsRequired is true.", nameof(minQuantity));

        if (minQuantity < 0)
            throw new ArgumentException("MinQuantity cannot be negative.", nameof(minQuantity));

        if (maxQuantity < minQuantity)
            throw new ArgumentException("MaxQuantity cannot be less than MinQuantity.", nameof(maxQuantity));

        return new MealSettings
        {
            Id = Guid.NewGuid(),
            MealTemplateId = mealTemplateId,
            CategoryId = categoryId,
            MinQuantity = minQuantity,
            MaxQuantity = maxQuantity,
            IsRequired = isRequired,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
