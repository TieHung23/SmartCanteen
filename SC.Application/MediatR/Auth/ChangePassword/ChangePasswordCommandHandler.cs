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
                    "Chưa đăng nhập.");
            }

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.Forbidden,
                    "Không tìm thấy người dùng.");
            }

            if (user.PasswordHash is null)
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.PasswordLoginUnavailable,
                    "Tài khoản này đăng nhập bằng Google nên không có mật khẩu.");
            }

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Result.Failure<PasswordActionResponse>(
                    Error.IncorrectCurrentPassword,
                    "Mật khẩu hiện tại không đúng.");
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

            const string message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return Result.Success(new PasswordActionResponse(message), message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error changing password for user {UserId}", currentUserService.UserId);
            return Result.Failure<PasswordActionResponse>(
                Error.ServerError,
                "Đã xảy ra lỗi khi đổi mật khẩu.");
        }
    }
}
