using SC.Domain.Domain.Verification.Enum;

namespace SC.Application.MediatR.Verification.GetMyStatus;

public record VerificationStatusResponse(
    Guid? RequestId,
    VerificationStatus? Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    bool HasOpenRequest);
