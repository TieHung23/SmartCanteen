using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.User.Enum;
using UserAggregate = SC.Domain.Domain.User.User;
using RefreshTokenAggregate = SC.Domain.Domain.User.RefreshToken;

namespace SC.Application.MediatR.User.Manager.UpdateAccountStatus;

internal class UpdateUserAccountStatusCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<RefreshTokenAggregate, Guid> refreshTokenRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateUserAccountStatusCommandHandler> logger)
    : ICommandHandler<UpdateUserAccountStatusCommand, UpdateUserAccountStatusResponse>
{
    public async Task<Result<UpdateUserAccountStatusResponse>> Handle(
        UpdateUserAccountStatusCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == Guid.Empty)
            {
                return Result.Failure<UpdateUserAccountStatusResponse>(
                    Error.InvalidValue,
                    "User id is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Result.Failure<UpdateUserAccountStatusResponse>(
                    Error.InvalidValue,
                    "Reason is required.");
            }

            if (request.UserId == currentUserService.UserId)
            {
                return Result.Failure<UpdateUserAccountStatusResponse>(
                    Error.InvalidValue,
                    "You cannot change your own account status.");
            }

            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null || user.IsDeleted)
            {
                return Result.Failure<UpdateUserAccountStatusResponse>(
                    Error.UserNotFound,
                    "User not found.");
            }

            if (request.Status is not (AccountStatus.Suspended or AccountStatus.Banned))
            {
                return Result.Failure<UpdateUserAccountStatusResponse>(
                    Error.InvalidValue,
                    "Only Suspended or Banned status can be set from this endpoint.");
            }

            var reason = request.Reason.Trim();
            if (request.Status == AccountStatus.Banned)
                user.Ban(reason);
            else
                user.Suspend(reason);

            var activeRefreshTokens = await refreshTokenRepository.FindListAsync(
                token => token.UserId == user.Id
                    && token.RevokedAt == null
                    && token.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);

            foreach (var refreshToken in activeRefreshTokens)
                refreshToken.Revoke();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            userRepository.Update(user);
            foreach (var refreshToken in activeRefreshTokens)
                refreshTokenRepository.Update(refreshToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var statusText = request.Status.ToString().ToLowerInvariant();
            var message = $"User account has been {statusText}.";
            var response = new UpdateUserAccountStatusResponse(
                user.Id,
                user.Status,
                activeRefreshTokens.Count,
                reason,
                message);

            return Result.Success(response, message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(
                ex,
                "Error updating user account status for user {UserId} to {Status}",
                request.UserId,
                request.Status);
            return Result.Failure<UpdateUserAccountStatusResponse>(
                Error.ServerError,
                "An error occurred while updating the user account status.");
        }
    }
}
