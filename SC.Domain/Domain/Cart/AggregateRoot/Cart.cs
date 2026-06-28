using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Cart.AggregateRoot;

public class Cart : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Cart() { }

    public Guid UserId { get; private set; }
    public string DataJson { get; private set; } = "{}";
    public long Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static Cart Create(Guid userId, string dataJson)
    {
        return new Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DataJson = dataJson,
            Version = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId
        };
    }

    public void Update(string dataJson, Guid updatedBy)
    {
        DataJson = dataJson;
        Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
