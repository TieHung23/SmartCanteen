using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Dish;
using SC.Domain.Domain.Session.Entity;

namespace SC.Domain.Domain.Session.AggregateRoot;

public class Session : AggregateRoot<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private readonly List<SessionDish> _sessionDishes = [];
    private readonly List<MealTemplate> _mealTemplates = [];

    private Session() { }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<SessionDish> SessionDishes => _sessionDishes.AsReadOnly();
    public IReadOnlyCollection<MealTemplate> MealTemplates => _mealTemplates.AsReadOnly();
    public DateTimeOffset AvailableFrom { get; private set; }
    public DateTimeOffset AvailableTo { get; private set; }
    public DateTimeOffset AvailableForOrder { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static Session Create(
        string name,
        string description,
        DateTimeOffset availableFrom,
        DateTimeOffset availableTo,
        DateTimeOffset availableForOrder,
        Guid createdBy)
    {
        return new Session
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            AvailableFrom = availableFrom,
            AvailableTo = availableTo,
            AvailableForOrder = availableForOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Update(
        string name,
        string description,
        DateTimeOffset availableFrom,
        DateTimeOffset availableTo,
        DateTimeOffset availableForOrder,
        bool isActive,
        Guid updatedBy)
    {
        Name = name;
        Description = description;
        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
        AvailableForOrder = availableForOrder;
        IsActive = isActive;
        Touch(updatedBy);
    }

    public void AddSessionDish(SessionDish sessionDish)
    {
        _sessionDishes.Add(sessionDish);
    }

    public void RemoveSessionDish(SessionDish sessionDish)
    {
        _sessionDishes.Remove(sessionDish);
    }

    public void AddMealTemplate(MealTemplate mealTemplate)
    {
        _mealTemplates.Add(mealTemplate);
    }

    public void RemoveMealTemplate(MealTemplate mealTemplate)
    {
        _mealTemplates.Remove(mealTemplate);
    }

    public void ClearMealTemplates()
    {
        _mealTemplates.Clear();
    }

    public void ClearSessionDishes()
    {
        _sessionDishes.Clear();
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
