using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Email;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Verification.AggregateRoot;
using SC.Domain.Domain.Verification.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Verification.Admin.Approve;

internal class ApproveVerificationCommandHandler(
    IRepositoryBase<VerificationRequest, Guid> verificationRepository,
    IRepositoryBase<UserAggregate, Guid> userRepository,
    ICurrentUserService currentUserService,
    IEmailSender emailSender,
    ILogger<ApproveVerificationCommandHandler> logger) : ICommandHandler<ApproveVerificationCommand, ApproveVerificationResponse>
{
    public async Task<Result<ApproveVerificationResponse>> Handle(ApproveVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var verification = await verificationRepository.FindByIdAsync(request.Id, cancellationToken);
            if (verification is null)
                return Result.Failure<ApproveVerificationResponse>(
                    Error.VerificationNotFound,
                    "Verification request not found.");

            if (verification.Status != VerificationStatus.Pending)
                return Result.Failure<ApproveVerificationResponse>(
                    Error.VerificationNotPending,
                    "Verification request is no longer pending.");

            var user = await userRepository.FindByIdAsync(verification.UserId, cancellationToken);
            if (user is null)
                return Result.Failure<ApproveVerificationResponse>(
                    Error.VerificationNotFound,
                    "Associated user no longer exists.");

            var reviewerId = currentUserService.UserId;
            verification.Approve(reviewerId);
            user.ActivateAfterIdentityApproved();

            var verifUpdate = await verificationRepository.UpdateAsync(verification);
            if (verifUpdate.IsFailure)
                return Result.Failure<ApproveVerificationResponse>(
                    verifUpdate.Error ?? Error.ServerError,
                    verifUpdate.Message);

            var userUpdate = await userRepository.UpdateAsync(user);
            if (userUpdate.IsFailure)
                return Result.Failure<ApproveVerificationResponse>(
                    userUpdate.Error ?? Error.ServerError,
                    userUpdate.Message);

            try
            {
                await emailSender.SendVerificationStatusAsync(user.Email, approved: true, rejectionReason: null, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send approval notification to {Email}", user.Email);
            }

            var response = new ApproveVerificationResponse
            {
                Id = request.Id,
                Message = "Verification approved."
            };

            return Result.Success(response, response.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ApproveVerificationResponse>(Error.InvalidValue, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error approving verification {Id}", request.Id);
            return Result.Failure<ApproveVerificationResponse>(
                Error.ServerError,
                "An error occurred while approving the verification.");
        }
    }
}
