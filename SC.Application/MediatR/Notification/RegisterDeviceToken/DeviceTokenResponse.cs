namespace SC.Application.MediatR.Notification.RegisterDeviceToken;

public sealed record DeviceTokenResponse(
    Guid Id,
    string Platform,
    string? DeviceId,
    string? AppVersion,
    bool IsActive,
    DateTimeOffset LastUsedAtUtc);
