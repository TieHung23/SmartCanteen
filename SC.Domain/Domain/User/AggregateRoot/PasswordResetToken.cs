using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.User;

public class PasswordResetToken : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private PasswordResetToken()
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

    public static PasswordResetToken Issue(Guid userId, string tokenHash, TimeSpan ttl)
    {
        return new PasswordResetToken
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
        if (!IsValid)
            throw new InvalidOperationException("Password reset token is invalid or expired.");

        ConsumedAt = DateTimeOffset.UtcNow;
    }
}
