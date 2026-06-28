using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using RefreshTokenAggregate = SC.Domain.Domain.User.RefreshToken;

namespace SC.Application.MediatR.Auth.Logout;

internal class LogoutCommandHandler(
    IGenericRepository<RefreshTokenAggregate, Guid> refreshTokenRepository,
    IJwtTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    ILogger<LogoutCommandHandler> logger) : ICommandHandler<LogoutCommand, LogoutResponse>
{
    public async Task<Result<LogoutResponse>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var hash = tokenGenerator.HashOpaqueToken(request.RefreshToken);

            var token = await refreshTokenRepository
                .FindSingleAsync(t => t.TokenHash == hash, cancellationToken);

            if (token is null)
            {
                var response = new LogoutResponse
                {
                    Message = "Logged out."
                };

                return Result.Success(response, response.Message);
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            token.Revoke();
            refreshTokenRepository.Update(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var revokedResponse = new LogoutResponse
            {
                Message = "Logged out."
            };

            return Result.Success(revokedResponse, revokedResponse.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error during logout");
            return Result.Failure<LogoutResponse>(
                Error.ServerError,
                "An error occurred while logging out.");
        }
    }
}
