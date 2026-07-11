using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.User.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.User.Manager.ReactivateUser;

internal class ReactivateUserCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<ReactivateUserCommandHandler> logger)
    : ICommandHandler<ReactivateUserCommand, ReactivateUserResponse>
{
    public async Task<Result<ReactivateUserResponse>> Handle(
        ReactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == Guid.Empty)
            {
                return Result.Failure<ReactivateUserResponse>(
                    Error.InvalidValue,
                    "User id is required.");
            }

            if (request.UserId == currentUserService.UserId)
            {
                return Result.Failure<ReactivateUserResponse>(
                    Error.InvalidValue,
                    "You cannot change your own account status.");
            }

            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null || user.IsDeleted)
            {
                return Result.Failure<ReactivateUserResponse>(
                    Error.UserNotFound,
                    "User not found.");
            }

            if (user.Status == AccountStatus.Banned)
            {
                return Result.Failure<ReactivateUserResponse>(
                    Error.InvalidValue,
                    "Banned accounts cannot be reactivated.");
            }

            if (user.Status != AccountStatus.Suspended)
            {
                return Result.Failure<ReactivateUserResponse>(
                    Error.InvalidValue,
                    "Only suspended accounts can be reactivated.");
            }

            user.Reactivate();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            const string message = "User account has been reactivated.";
            return Result.Success(
                new ReactivateUserResponse(user.Id, user.Status, message),
                message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error reactivating user {UserId}", request.UserId);
            return Result.Failure<ReactivateUserResponse>(
                Error.ServerError,
                "An error occurred while reactivating the user account.");
        }
    }
}
