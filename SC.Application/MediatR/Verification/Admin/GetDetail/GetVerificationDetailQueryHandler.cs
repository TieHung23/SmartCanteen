using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Verification.AggregateRoot;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Verification.Admin.GetDetail;

internal class GetVerificationDetailQueryHandler(
    IGenericRepository<VerificationRequest, Guid> verificationRepository,
    IGenericRepository<UserAggregate, Guid> userRepository,
    ILogger<GetVerificationDetailQueryHandler> logger)
    : IQueryHandler<GetVerificationDetailQuery, VerificationDetailResponse>
{
    public async Task<Result<VerificationDetailResponse>> Handle(
        GetVerificationDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var verification = await verificationRepository
                .GetQueryable(v => v.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (verification is null)
                return Result.Failure<VerificationDetailResponse>(Error.VerificationNotFound, "Verification request not found.");

            var user = await userRepository.GetByIdAsync(verification.UserId, cancellationToken);

            var docs = verification.Documents
                .Select(d => new VerificationDocumentDto(
                    d.Id,
                    d.DocumentType,
                    d.CloudinaryUrl,
                    d.FileName,
                    d.FileSize,
                    d.MimeType,
                    d.UploadedAt))
                .ToList();

            var response = new VerificationDetailResponse(
                verification.Id,
                verification.UserId,
                user?.Email ?? string.Empty,
                user?.Name ?? string.Empty,
                user?.StudentId,
                user?.MajorOrClass,
                user?.DateOfBirth,
                verification.Status,
                verification.SubmittedAt,
                verification.ReviewedAt,
                verification.ReviewedBy,
                verification.RejectionReason,
                verification.ExpiresAt,
                docs);

            return Result.Success(response, "Verification detail retrieved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving verification detail {Id}", request.Id);
            return Result.Failure<VerificationDetailResponse>(
                Error.ServerError,
                "An error occurred while retrieving the verification detail.");
        }
    }
}
