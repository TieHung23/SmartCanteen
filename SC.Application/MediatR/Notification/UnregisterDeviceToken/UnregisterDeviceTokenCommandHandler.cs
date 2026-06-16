using Microsoft.EntityFrameworkCore;
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

            var query = deviceTokenRepository
                .GetQueryable(token =>
                    token.UserId == userId
                    && token.IsActive
                    && !token.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Token))
            {
                var tokenHash = UserDeviceToken.ComputeTokenHash(request.Token);
                query = query.Where(token => token.TokenHash == tokenHash);
            }
            else
            {
                var deviceId = request.DeviceId!.Trim();
                query = query.Where(token => token.DeviceId == deviceId);
            }

            var tokens = await query.ToListAsync(cancellationToken);
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
