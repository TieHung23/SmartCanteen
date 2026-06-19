using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Session.Entity;

public class MealSettings : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private MealSettings() { }

    public Guid MealTemplateId { get; private set; }
    public Guid CategoryId { get; private set; }
    public int MinQuantity { get; private set; }
    public int MaxQuantity { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

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

    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
}
