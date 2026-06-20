using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Setting.AggregateRoot;

public class Setting : AggregateRoot<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private Setting()
    {
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Group { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static Setting Create(
        string code,
        string name,
        string description,
        string group,
        string scope,
        string value,
        string type,
        Guid createdBy)
    {
        return new Setting
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Group = group,
            Scope = scope,
            Value = value,
            Type = type,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(
        string name,
        string description,
        string value,
        string type,
        Guid updatedBy)
    {
        Name = name;
        Description = description;
        Value = value;
        Type = type;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Update(
        string name,
        string description,
        string group,
        string scope,
        string value,
        string type,
        Guid updatedBy)
    {
        Name = name;
        Description = description;
        Group = group;
        Scope = scope;
        Value = value;
        Type = type;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }

    public void SoftDelete(Guid updatedBy)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
