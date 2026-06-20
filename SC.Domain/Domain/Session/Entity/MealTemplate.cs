using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Session.Entity;

public class MealTemplate : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private readonly List<MealSettings> _settings = [];

    private MealTemplate() { }

    public Guid SessionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<MealSettings> Settings => _settings.AsReadOnly();
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static MealTemplate Create(Guid sessionId, string name)
    {
        return new MealTemplate
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = name,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void AddSetting(Guid categoryId, int minQuantity, int maxQuantity, bool isRequired)
    {
        _settings.Add(MealSettings.Create(Id, categoryId, minQuantity, maxQuantity, isRequired));
    }

    public void ClearSettings()
    {
        _settings.Clear();
    }

    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
}
