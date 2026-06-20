using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Domain.Dish.AggregateRoot;

public class Dish : AggregateRoot<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private Dish() { }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Create(0);
    public bool IsActive { get; private set; } = true;
    public Guid CategoryId { get; private set; }
    public string? ImgUrl { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static Dish Create(
        string name,
        string description,
        Money price,
        Guid categoryId,
        Guid createdBy,
        string? imgUrl = null)
    {
        return new Dish
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            ImgUrl = imgUrl,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Update(
        string name,
        string description,
        Money price,
        Guid categoryId,
        bool isActive,
        Guid updatedBy,
        string? imgUrl = null)
    {
        Name = name;
        Description = description;
        Price = price;
        CategoryId = categoryId;
        IsActive = isActive;
        ImgUrl = imgUrl;
        Touch(updatedBy);
    }

    public void MarkInactive(Guid updatedBy)
    {
        IsActive = false;
        Touch(updatedBy);
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
