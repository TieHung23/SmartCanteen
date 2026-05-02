using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Category;

public class Category : Entity<Guid>, IAuditableEntity<Guid>
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
}