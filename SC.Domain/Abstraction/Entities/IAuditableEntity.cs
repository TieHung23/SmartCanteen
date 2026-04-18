namespace SC.Domain.Abstraction.Entities;

public interface IAuditableEntity<T>
{
    DateTimeOffset CreatedAtUtc { get; set; }
    DateTimeOffset? UpdatedAtUtc { get; set; }
    T CreatedBy { get; set; }
    T? UpdatedBy { get; set; }
}