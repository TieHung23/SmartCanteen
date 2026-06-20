using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Verification.AggregateRoot;

namespace SC.Application.MediatR.Verification.GetMyStatus;

internal class GetMyVerificationStatusQueryHandler(
    IGenericRepository<VerificationRequest, Guid> verificationRepository,
    ICurrentUserService currentUserService,
    ILogger<GetMyVerificationStatusQueryHandler> logger)
    : IQueryHandler<GetMyVerificationStatusQuery, VerificationStatusResponse>
{
    public async Task<Result<VerificationStatusResponse>> Handle(
        GetMyVerificationStatusQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
                return Result.Failure<VerificationStatusResponse>(Error.Forbidden, "Not authenticated.");

            var allRequests = await verificationRepository
                .FindListAsync(v => v.UserId == userId, cancellationToken);
            var latest = allRequests.OrderByDescending(v => v!.SubmittedAt).FirstOrDefault();

            if (latest is null)
            {
                return Result.Success(
                    new VerificationStatusResponse(null, null, null, null, null, false),
                    "No verification request found.");
            }

            var response = new VerificationStatusResponse(
                latest.Id,
                latest.Status,
                latest.SubmittedAt,
                latest.ReviewedAt,
                latest.RejectionReason,
                HasOpenRequest: latest.Status == Domain.Domain.Verification.Enum.VerificationStatus.Pending);

            return Result.Success(response, "Verification status retrieved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving verification status");
            return Result.Failure<VerificationStatusResponse>(
                Error.ServerError,
                "An error occurred while retrieving the verification status.");
        }
    }
}
