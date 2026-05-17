using SC.Contract.Abstraction.Message;
using SC.Domain.Domain.Verification.Enum;

namespace SC.Application.MediatR.Verification.Admin.GetDetail;

public record GetVerificationDetailQuery(Guid Id) : IQuery<VerificationDetailResponse>;

public record VerificationDetailResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    string? StudentId,
    string? MajorOrClass,
    DateOnly? DateOfBirth,
    VerificationStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedBy,
    string? RejectionReason,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<VerificationDocumentDto> Documents);

public record VerificationDocumentDto(
    Guid Id,
    DocumentType DocumentType,
    string CloudinaryUrl,
    string FileName,
    long FileSize,
    string MimeType,
    DateTimeOffset UploadedAt);
