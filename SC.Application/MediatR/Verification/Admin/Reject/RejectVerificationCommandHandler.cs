using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Email;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Verification.AggregateRoot;
using SC.Domain.Domain.Verification.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Verification.Admin.Reject;

internal class RejectVerificationCommandHandler(
    IRepositoryBase<VerificationRequest, Guid> verificationRepository,
    IRepositoryBase<UserAggregate, Guid> userRepository,
    ICurrentUserService currentUserService,
    IEmailSender emailSender,
    ILogger<RejectVerificationCommandHandler> logger) : ICommandHandler<RejectVerificationCommand>
{
    public async Task<Result> Handle(RejectVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Result.Failure(Error.RejectionReasonRequired, "Rejection reason is required.");

            var verification = await verificationRepository.FindByIdAsync(request.Id, cancellationToken);
            if (verification is null)
                return Result.Failure(Error.VerificationNotFound, "Verification request not found.");

            if (verification.Status != VerificationStatus.Pending)
                return Result.Failure(Error.VerificationNotPending, "Verification request is no longer pending.");

            var reviewerId = currentUserService.UserId;
            verification.Reject(reviewerId, request.Reason);

            var update = await verificationRepository.UpdateAsync(verification);
            if (update.IsFailure)
                return Result.Failure(update.Error ?? Error.ServerError, update.Message);

            var user = await userRepository.FindByIdAsync(verification.UserId, cancellationToken);
            if (user is not null)
            {
                try
                {
                    await emailSender.SendVerificationStatusAsync(user.Email, approved: false, rejectionReason: request.Reason, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send rejection notification to {Email}", user.Email);
                }
            }

            return Result.Success("Verification rejected.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rejecting verification {Id}", request.Id);
            return Result.Failure(Error.ServerError, "An error occurred while rejecting the verification.");
        }
    }
}
