using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.User.Enum;
using UserAggregate = SC.Domain.Domain.User.User;
using RefreshTokenAggregate = SC.Domain.Domain.User.RefreshToken;

namespace SC.Application.MediatR.Auth.Refresh;

internal class RefreshTokenCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<RefreshTokenAggregate, Guid> refreshTokenRepository,
    IJwtTokenGenerator tokenGenerator,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    ILogger<RefreshTokenCommandHandler> logger) : ICommandHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var providedHash = tokenGenerator.HashOpaqueToken(request.RefreshToken);

            var existing = await refreshTokenRepository
                .FindSingleAsync(t => t.TokenHash == providedHash, cancellationToken);

            if (existing is null || !existing.IsActive)
            {
                return Result.Failure<AuthTokensDto>(Error.InvalidRefreshToken, "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var user = await userRepository.GetByIdAsync(existing.UserId, cancellationToken);
            if (user is null)
            {
                existing.Revoke();
                refreshTokenRepository.Update(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Failure<AuthTokensDto>(Error.InvalidRefreshToken, "Tài khoản không còn hợp lệ.");
            }

            if (user.Status == AccountStatus.Banned)
            {
                existing.Revoke();
                refreshTokenRepository.Update(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Failure<AuthTokensDto>(
                    Error.AccountBanned,
                    "Tài khoản này đã bị cấm.",
                    user.StatusReason);
            }

            if (user.Status == AccountStatus.Suspended)
            {
                existing.Revoke();
                refreshTokenRepository.Update(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Failure<AuthTokensDto>(
                    Error.AccountSuspended,
                    "Tài khoản này đã bị tạm khóa.",
                    user.StatusReason);
            }

            var newOpaque = tokenGenerator.GenerateOpaqueToken();
            var ttl = TimeSpan.FromDays(configuration.GetValue("Jwt:RefreshTokenDays", 7));
            var newRefresh = RefreshTokenAggregate.Issue(user.Id, newOpaque.TokenHash, ttl);

            existing.Revoke(newRefresh.Id);

            await refreshTokenRepository.AddAsync(newRefresh, cancellationToken);
            refreshTokenRepository.Update(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var access = tokenGenerator.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.Status == AccountStatus.Active);

            return Result.Success(
                new AuthTokensDto(
                    access.Token,
                    access.ExpiresAt,
                    newOpaque.RawToken,
                    newRefresh.ExpiresAt),
                "Làm mới phiên đăng nhập thành công.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error refreshing token");
            return Result.Failure<AuthTokensDto>(Error.ServerError, "Đã xảy ra lỗi khi làm mới phiên đăng nhập.");
        }
    }
}
