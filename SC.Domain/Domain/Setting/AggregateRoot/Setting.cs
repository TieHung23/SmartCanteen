using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Setting.AggregateRoot;

public class Setting : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Setting()
    {
    }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Setting Create(string code, string name, string description, string group, string value, string type, Guid createdBy)
    {
        return new Setting
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Group = group,
            Value = value,
            Type = type,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string description, string value, string type, Guid updatedBy)
    {
        Name = name;
        Description = description;
        Value = value;
        Type = type;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(Guid updatedBy)
    {
        IsDeleted = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
