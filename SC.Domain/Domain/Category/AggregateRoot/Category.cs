using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Category.AggregateRoot;

public class Category : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Category()
    {
    }
    
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Category Create(string name, string description, Guid createdBy)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Factory method for creating categories with specific IDs (used for seeding)
    /// </summary>
    internal static Category CreateForSeeding(Guid id, string name, string description, Guid createdBy, DateTimeOffset createdAtUtc)
    {
        return new Category
        {
            Id = id,
            Name = name,
            Description = description,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = createdBy,
            UpdatedBy = Guid.Empty
        };
    }
}