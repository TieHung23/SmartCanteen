using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Notification;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Notification.Entity;

namespace SC.Application.MediatR.Notification.RegisterDeviceToken;

internal sealed class RegisterDeviceTokenCommandHandler(
    IGenericRepository<UserDeviceToken, Guid> deviceTokenRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<RegisterDeviceTokenCommandHandler> logger)
    : ICommandHandler<RegisterDeviceTokenCommand, DeviceTokenResponse>
{
    public async Task<Result<DeviceTokenResponse>> Handle(
        RegisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Result.Failure<DeviceTokenResponse>(
                    Error.InvalidCredentials,
                    "Authenticated user was not found.");
            }

            var tokenHash = UserDeviceToken.ComputeTokenHash(request.Token);
            var deviceToken = await deviceTokenRepository
                .GetQueryable(token => token.TokenHash == tokenHash)
                .SingleOrDefaultAsync(cancellationToken);

            if (deviceToken is null
                && !string.IsNullOrWhiteSpace(request.DeviceId))
            {
                var deviceId = request.DeviceId.Trim();
                deviceToken = await deviceTokenRepository
                    .GetQueryable(token =>
                        token.UserId == userId
                        && token.DeviceId == deviceId)
                    .OrderByDescending(token => token.LastUsedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (deviceToken is null)
            {
                deviceToken = UserDeviceToken.Register(
                    userId,
                    request.Token,
                    NormalizePlatform(request.Platform),
                    request.DeviceId,
                    request.AppVersion,
                    userId);

                await deviceTokenRepository.AddAsync(deviceToken, cancellationToken);
            }
            else
            {
                deviceToken.Refresh(
                    userId,
                    request.Token,
                    NormalizePlatform(request.Platform),
                    request.DeviceId,
                    request.AppVersion,
                    userId);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                ToResponse(deviceToken),
                "Device token registered successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register device token");
            return Result.Failure<DeviceTokenResponse>(
                Error.ServerError,
                "An error occurred while registering the device token.");
        }
    }

    private static string NormalizePlatform(string platform)
    {
        return platform.Trim().Equals(DeviceTokenPlatforms.Ios, StringComparison.OrdinalIgnoreCase)
            ? DeviceTokenPlatforms.Ios
            : DeviceTokenPlatforms.Android;
    }

    private static DeviceTokenResponse ToResponse(UserDeviceToken token)
    {
        return new DeviceTokenResponse(
            token.Id,
            token.Platform,
            token.DeviceId,
            token.AppVersion,
            token.IsActive,
            token.LastUsedAtUtc);
    }
}
