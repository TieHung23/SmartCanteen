using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using UserAggregate = SC.Domain.Domain.User.User;
using RefreshTokenAggregate = SC.Domain.Domain.User.RefreshToken;

namespace SC.Application.MediatR.Auth.ChangePassword;

internal class ChangePasswordCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<RefreshTokenAggregate, Guid> refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<ChangePasswordCommandHandler> logger)
    : ICommandHandler<ChangePasswordCommand, PasswordActionResponse>
{
    public async Task<Result<PasswordActionResponse>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.Forbidden,
                    "Not authenticated.");
            }

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.Forbidden,
                    "User not found.");
            }

            if (user.PasswordHash is null)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.PasswordLoginUnavailable,
                    "This account uses Google sign-in and does not have a password.");
            }

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.IncorrectCurrentPassword,
                    "Current password is incorrect.");
            }

            var activeRefreshTokens = await refreshTokenRepository
                .FindListAsync(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);

            user.ChangePassword(passwordHasher.Hash(request.NewPassword));
            foreach (var refreshToken in activeRefreshTokens)
                refreshToken.Revoke();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            userRepository.Update(user);
            foreach (var refreshToken in activeRefreshTokens)
                refreshTokenRepository.Update(refreshToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            const string message = "Password changed successfully. Please sign in again.";
            return Result.Success(new PasswordActionResponse(message), message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error changing password for user {UserId}", currentUserService.UserId);
            return Result.Failure<PasswordActionResponse>(
                Error.ServerError,
                "An error occurred while changing the password.");
        }
    }
}
