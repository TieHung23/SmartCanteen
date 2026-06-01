using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using RefreshTokenAggregate = SC.Domain.Domain.User.RefreshToken;

namespace SC.Application.MediatR.Auth.Logout;

internal class LogoutCommandHandler(
    IRepositoryBase<RefreshTokenAggregate, Guid> refreshTokenRepository,
    IJwtTokenGenerator tokenGenerator,
    ILogger<LogoutCommandHandler> logger) : ICommandHandler<LogoutCommand, LogoutResponse>
{
    public async Task<Result<LogoutResponse>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var hash = tokenGenerator.HashOpaqueToken(request.RefreshToken);

            var token = await refreshTokenRepository
                .FindAll(t => t!.TokenHash == hash, cancellationToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (token is null)
            {
                // Silently succeed — logging out an unknown token still leaves the
                // user logged out from their perspective.
                var response = new LogoutResponse
                {
                    Message = "Logged out."
                };

                return Result.Success(response, response.Message);
            }

            token.Revoke();
            await refreshTokenRepository.UpdateAsync(token);
            var revokedResponse = new LogoutResponse
            {
                Message = "Logged out."
            };

            return Result.Success(revokedResponse, revokedResponse.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
            return Result.Failure<LogoutResponse>(
                Error.ServerError,
                "An error occurred while logging out.");
        }
    }
}
