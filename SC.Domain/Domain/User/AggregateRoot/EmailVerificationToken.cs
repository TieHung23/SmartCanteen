using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.User;

public class EmailVerificationToken : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private EmailVerificationToken()
    {
    }

    public required Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsValid => !IsConsumed && !IsExpired;

    public static EmailVerificationToken Issue(Guid userId, string tokenHash, TimeSpan ttl)
    {
        return new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Consume()
    {
        if (IsConsumed) throw new InvalidOperationException("Verification token has already been consumed.");
        if (IsExpired) throw new InvalidOperationException("Verification token has expired.");
        ConsumedAt = DateTimeOffset.UtcNow;
    }
}
