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
                .FindSingleAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (token is null || !token.IsValid)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.InvalidOrExpiredToken,
                    "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
            }

            var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
            if (user is null || user.PasswordHash is null)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.InvalidOrExpiredToken,
                    "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
            }

            var activeRefreshTokens = await refreshTokenRepository
                .FindListAsync(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);

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

            const string message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.";
            return Result.Success(new PasswordActionResponse(message), message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error resetting password");
            return Result.Failure<PasswordActionResponse>(
                Error.ServerError,
                "Đã xảy ra lỗi khi đặt lại mật khẩu.");
        }
    }
}
