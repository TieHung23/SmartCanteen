using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Verification.Enum;
using SC.Domain.Domain.Verification.ValueObject;

namespace SC.Domain.Domain.Verification.AggregateRoot;

public class VerificationRequest : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private readonly List<VerificationDocument> _documents = new();

    private VerificationRequest()
    {
    }

    public required Guid UserId { get; set; }
    public required VerificationStatus Status { get; set; }
    public required DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }

    public IReadOnlyCollection<VerificationDocument> Documents => _documents.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static VerificationRequest Submit(
        Guid userId,
        IEnumerable<VerificationDocument> documents,
        TimeSpan ttl)
    {
        if (documents is null) throw new ArgumentNullException(nameof(documents));

        var docs = documents.ToList();
        if (docs.Count == 0)
            throw new ArgumentException("At least one document is required.", nameof(documents));

        var now = DateTimeOffset.UtcNow;
        var request = new VerificationRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = VerificationStatus.Pending,
            SubmittedAt = now,
            ExpiresAt = now.Add(ttl),
            CreatedAtUtc = now
        };

        foreach (var doc in docs)
        {
            doc.VerificationRequestId = request.Id;
            request._documents.Add(doc);
        }

        return request;
    }

    public void Approve(Guid reviewerId)
    {
        EnsurePending();
        Status = VerificationStatus.Approved;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewedBy = reviewerId;
    }

    public void Reject(Guid reviewerId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        EnsurePending();
        Status = VerificationStatus.Rejected;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewedBy = reviewerId;
        RejectionReason = reason;
    }

    public void MarkExpired()
    {
        EnsurePending();
        Status = VerificationStatus.Expired;
    }

    private void EnsurePending()
    {
        if (Status != VerificationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Verification request is already {Status} and cannot be modified.");
        }
    }
}
