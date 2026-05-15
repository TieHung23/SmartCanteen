using SC.Domain.Domain.Verification.Enum;

namespace SC.Application.MediatR.Verification.SubmitVerification;

public record VerificationFileInput(
    Stream Content,
    string FileName,
    long FileSize,
    string MimeType,
    DocumentType DocumentType);
