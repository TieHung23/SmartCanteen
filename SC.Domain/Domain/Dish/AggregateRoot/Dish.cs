using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.SharedKernel.ValueObjects;
using CategoryAggregate = SC.Domain.Domain.Category.AggregateRoot.Category;

namespace SC.Domain.Domain.Dish.AggregateRoot;

public class Dish : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Dish()
    {
    }

    public required string Name { get; set; }
    public required string Description { get; set; }
    public Money Price { get; set; } = Money.Create(0);
    public bool IsActive { get; set; } = true;
    public Guid CategoryId { get; set; }
    public CategoryAggregate Category { get; set; } = null!;
    public string? ImgUrl { get; set; }
    public ICollection<SessionDish> SessionDishes { get; set; } = new List<SessionDish>();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

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
            CreatedBy = createdBy
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
