namespace SC.Domain.Abstraction.Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAtUtc { get; }
    void SoftDelete();
}
