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
    IGenericRepository<VerificationRequest, Guid> verificationRepository,
    IGenericRepository<UserAggregate, Guid> userRepository,
    ICurrentUserService currentUserService,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    ILogger<ApproveVerificationCommandHandler> logger) : ICommandHandler<ApproveVerificationCommand, ApproveVerificationResponse>
{
    public async Task<Result<ApproveVerificationResponse>> Handle(ApproveVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var verification = await verificationRepository.GetByIdAsync(request.Id, cancellationToken);
            if (verification is null)
                return Result.Failure<ApproveVerificationResponse>(
                    Error.VerificationNotFound,
                    "Verification request not found.");

            if (verification.Status != VerificationStatus.Pending)
                return Result.Failure<ApproveVerificationResponse>(
                    Error.VerificationNotPending,
                    "Verification request is no longer pending.");

            var user = await userRepository.GetByIdAsync(verification.UserId, cancellationToken);
            if (user is null)
                return Result.Failure<ApproveVerificationResponse>(
                    Error.VerificationNotFound,
                    "Associated user no longer exists.");

            var reviewerId = currentUserService.UserId;
            verification.Approve(reviewerId);
            user.ActivateAfterIdentityApproved();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            verificationRepository.Update(verification);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

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
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure<ApproveVerificationResponse>(Error.InvalidValue, ex.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error approving verification {Id}", request.Id);
            return Result.Failure<ApproveVerificationResponse>(
                Error.ServerError,
                "An error occurred while approving the verification.");
        }
    }
}
