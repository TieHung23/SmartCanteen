using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Notification.Entity;

namespace SC.Application.MediatR.Notification.UnregisterDeviceToken;

internal sealed class UnregisterDeviceTokenCommandHandler(
    IGenericRepository<UserDeviceToken, Guid> deviceTokenRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UnregisterDeviceTokenCommandHandler> logger)
    : ICommandHandler<UnregisterDeviceTokenCommand, UnregisterDeviceTokenResponse>
{
    public async Task<Result<UnregisterDeviceTokenResponse>> Handle(
        UnregisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Result.Failure<UnregisterDeviceTokenResponse>(
                    Error.InvalidCredentials,
                    "Authenticated user was not found.");
            }

            var tokens = await deviceTokenRepository
                .FindListAsync(token =>
                    token.UserId == userId
                    && token.IsActive
                    && !token.IsDeleted, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Token))
            {
                var tokenHash = UserDeviceToken.ComputeTokenHash(request.Token);
                tokens = tokens.Where(token => token.TokenHash == tokenHash).ToList();
            }
            else
            {
                var deviceId = request.DeviceId!.Trim();
                tokens = tokens.Where(token => token.DeviceId == deviceId).ToList();
            }
            foreach (var token in tokens)
            {
                token.Revoke(userId);
            }

            if (tokens.Count > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success(
                new UnregisterDeviceTokenResponse(tokens.Count),
                "Device token unregistered successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unregister device token");
            return Result.Failure<UnregisterDeviceTokenResponse>(
                Error.ServerError,
                "An error occurred while unregistering the device token.");
        }
    }
}
