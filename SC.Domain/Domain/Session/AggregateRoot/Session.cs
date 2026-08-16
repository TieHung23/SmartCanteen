using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Dish;
using SC.Domain.Domain.Session.Entity;
using SC.Domain.Domain.Session.Enum;

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

    public DateTimeOffset? FinalizationDeadline { get; private set; }
    public AutoFinalizePolicy AutoFinalizePolicy { get; private set; } = AutoFinalizePolicy.AutoReject;
    public bool IsFinalized { get; private set; }
    public DateTimeOffset? FinalizedAtUtc { get; private set; }

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

    public void ConfigureFinalization(DateTimeOffset? deadline, AutoFinalizePolicy autoPolicy, Guid updatedBy)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Session is already finalized.");
        if (deadline.HasValue && deadline.Value <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Finalization deadline must be in the future.", nameof(deadline));

        FinalizationDeadline = deadline;
        AutoFinalizePolicy = autoPolicy;
        Touch(updatedBy);
    }

    public void Finalize(Guid updatedBy)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Session is already finalized.");
        if (FinalizationDeadline.HasValue && DateTimeOffset.UtcNow > FinalizationDeadline.Value)
            throw new InvalidOperationException("Finalization deadline has passed.");

        IsFinalized = true;
        FinalizedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    public void AutoFinalize()
    {
        if (IsFinalized) return;
        IsFinalized = true;
        FinalizedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Demo/ops helper: pulls the serving window's start forward to now so queued
    /// serving jobs become eligible for robot pickup immediately instead of waiting
    /// for the originally scheduled AvailableFrom. No-op if serving already started.
    /// </summary>
    public void StartServingNow(Guid updatedBy)
    {
        var now = DateTimeOffset.UtcNow;
        if (AvailableFrom > now)
        {
            AvailableFrom = now;
            Touch(updatedBy);
        }
    }

    /// <summary>
    /// Closes the finalize window at the moment the session actually got finalized. Without this a
    /// session finalized ahead of schedule keeps advertising a deadline in the future, so listings,
    /// report timelines and any FE countdown still show time left to do something that is already
    /// done. Must be called *after* Finalize() - Finalize() rejects a deadline that has passed, so
    /// pulling the deadline in first would make the very next call throw. No-op when no deadline was
    /// configured (nothing to close) or when it has already passed; never pushes a deadline back.
    /// </summary>
    public void CloseFinalizationWindowNow(Guid updatedBy)
    {
        var now = DateTimeOffset.UtcNow;
        if (!FinalizationDeadline.HasValue || FinalizationDeadline.Value <= now)
            return;

        FinalizationDeadline = now;
        Touch(updatedBy);
    }

    /// <summary>
    /// Marks the session inactive once its serving window has closed, so it stops showing up as an
    /// operating session (listings, "is this session still running" checks) even though the time
    /// window alone already blocks new orders/robot pulls. No-op if already inactive.
    /// </summary>
    public void MarkInactive(Guid updatedBy)
    {
        if (!IsActive) return;
        IsActive = false;
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
