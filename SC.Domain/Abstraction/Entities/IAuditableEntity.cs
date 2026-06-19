namespace SC.Domain.Abstraction.Entities;

public interface IAuditableEntity<T>
{
    DateTimeOffset CreatedAtUtc { get; }
    DateTimeOffset? UpdatedAtUtc { get; }
    T CreatedBy { get; }
    T? UpdatedBy { get; }
}
