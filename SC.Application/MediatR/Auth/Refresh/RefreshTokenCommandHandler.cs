using Microsoft.EntityFrameworkCore;
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
    IRepositoryBase<UserAggregate, Guid> userRepository,
    IRepositoryBase<RefreshTokenAggregate, Guid> refreshTokenRepository,
    IJwtTokenGenerator tokenGenerator,
    IConfiguration configuration,
    ILogger<RefreshTokenCommandHandler> logger) : ICommandHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var providedHash = tokenGenerator.HashOpaqueToken(request.RefreshToken);

            var existing = await refreshTokenRepository
                .FindAll(t => t!.TokenHash == providedHash, cancellationToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null || !existing.IsActive)
            {
                return Result.Failure<AuthTokensDto>(Error.InvalidRefreshToken, "Refresh token is invalid or expired.");
            }

            var user = await userRepository.FindByIdAsync(existing.UserId, cancellationToken);
            if (user is null || user.Status is AccountStatus.Suspended or AccountStatus.Banned)
            {
                existing.Revoke();
                await refreshTokenRepository.UpdateAsync(existing);
                return Result.Failure<AuthTokensDto>(Error.InvalidRefreshToken, "Account no longer eligible.");
            }

            var newOpaque = tokenGenerator.GenerateOpaqueToken();
            var ttl = TimeSpan.FromDays(configuration.GetValue("Jwt:RefreshTokenDays", 7));
            var newRefresh = RefreshTokenAggregate.Issue(user.Id, newOpaque.TokenHash, ttl);

            existing.Revoke(newRefresh.Id);

            var addResult = await refreshTokenRepository.AddAsync(newRefresh);
            if (addResult.IsFailure)
                return Result.Failure<AuthTokensDto>(addResult.Error ?? Error.ServerError, addResult.Message);

            var updateResult = await refreshTokenRepository.UpdateAsync(existing);
            if (updateResult.IsFailure)
                return Result.Failure<AuthTokensDto>(updateResult.Error ?? Error.ServerError, updateResult.Message);

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
                "Token refreshed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing token");
            return Result.Failure<AuthTokensDto>(Error.ServerError, "An error occurred while refreshing the token.");
        }
    }
}
