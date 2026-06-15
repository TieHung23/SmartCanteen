using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using UserAggregate = SC.Domain.Domain.User.User;
using PasswordResetTokenAggregate = SC.Domain.Domain.User.PasswordResetToken;
using RefreshTokenAggregate = SC.Domain.Domain.User.RefreshToken;

namespace SC.Application.MediatR.Auth.ResetPassword;

internal class ResetPasswordCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<PasswordResetTokenAggregate, Guid> tokenRepository,
    IGenericRepository<RefreshTokenAggregate, Guid> refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    ILogger<ResetPasswordCommandHandler> logger)
    : ICommandHandler<ResetPasswordCommand, PasswordActionResponse>
{
    public async Task<Result<PasswordActionResponse>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tokenHash = tokenGenerator.HashOpaqueToken(request.Token);
            var token = await tokenRepository
                .GetQueryable(t => t.TokenHash == tokenHash)
                .FirstOrDefaultAsync(cancellationToken);

            if (token is null || !token.IsValid)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.InvalidOrExpiredToken,
                    "The password reset token is invalid or expired.");
            }

            var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
            if (user is null || user.PasswordHash is null)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.InvalidOrExpiredToken,
                    "The password reset token is invalid or expired.");
            }

            var activeRefreshTokens = await refreshTokenRepository
                .GetQueryable(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync(cancellationToken);

            user.ChangePassword(passwordHasher.Hash(request.NewPassword));
            token.Consume();
            foreach (var refreshToken in activeRefreshTokens)
                refreshToken.Revoke();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            userRepository.Update(user);
            tokenRepository.Update(token);
            foreach (var refreshToken in activeRefreshTokens)
                refreshTokenRepository.Update(refreshToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            const string message = "Password reset successfully. Please sign in again.";
            return Result.Success(new PasswordActionResponse(message), message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error resetting password");
            return Result.Failure<PasswordActionResponse>(
                Error.ServerError,
                "An error occurred while resetting the password.");
        }
    }
}
