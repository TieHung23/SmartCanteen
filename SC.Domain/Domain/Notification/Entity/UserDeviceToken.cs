using System.Security.Cryptography;
using System.Text;
using SC.Domain.Abstraction.Entities;
using SC.Domain.SharedKernel;

namespace SC.Domain.Domain.Notification.Entity;

public sealed class UserDeviceToken : Entity<Guid>, IAuditableEntity<Guid>
{
    private UserDeviceToken()
    {
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public string Platform { get; private set; } = string.Empty;
    public string? DeviceId { get; private set; }
    public string? AppVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset LastUsedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static UserDeviceToken Register(
        Guid userId,
        string token,
        string platform,
        string? deviceId,
        string? appVersion,
        Guid createdBy)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        ValidateRequired(token, nameof(token), DeviceTokenConstraints.TokenMaxLength);
        ValidateRequired(platform, nameof(platform), DeviceTokenConstraints.PlatformMaxLength);
        ValidateOptional(deviceId, nameof(deviceId), DeviceTokenConstraints.DeviceIdMaxLength);
        ValidateOptional(appVersion, nameof(appVersion), DeviceTokenConstraints.AppVersionMaxLength);

        var now = DateTimeOffset.UtcNow;
        return new UserDeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token.Trim(),
            TokenHash = ComputeTokenHash(token),
            Platform = platform.Trim(),
            DeviceId = NormalizeOptional(deviceId),
            AppVersion = NormalizeOptional(appVersion),
            IsActive = true,
            LastUsedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Refresh(
        Guid userId,
        string token,
        string platform,
        string? deviceId,
        string? appVersion,
        Guid updatedBy)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        ValidateRequired(token, nameof(token), DeviceTokenConstraints.TokenMaxLength);
        ValidateRequired(platform, nameof(platform), DeviceTokenConstraints.PlatformMaxLength);
        ValidateOptional(deviceId, nameof(deviceId), DeviceTokenConstraints.DeviceIdMaxLength);
        ValidateOptional(appVersion, nameof(appVersion), DeviceTokenConstraints.AppVersionMaxLength);

        UserId = userId;
        Token = token.Trim();
        TokenHash = ComputeTokenHash(token);
        Platform = platform.Trim();
        DeviceId = NormalizeOptional(deviceId);
        AppVersion = NormalizeOptional(appVersion);
        IsActive = true;
        IsDeleted = false;
        RevokedAtUtc = null;
        LastUsedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = LastUsedAtUtc;
        UpdatedBy = updatedBy;
    }

    public void MarkUsed(Guid updatedBy)
    {
        LastUsedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = LastUsedAtUtc;
        UpdatedBy = updatedBy;
    }

    public void Revoke(Guid updatedBy)
    {
        if (!IsActive && IsDeleted)
        {
            return;
        }

        IsActive = false;
        RevokedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = RevokedAtUtc;
        UpdatedBy = updatedBy;
        SoftDelete();
    }

    public static string ComputeTokenHash(string token)
    {
        ValidateRequired(token, nameof(token), DeviceTokenConstraints.TokenMaxLength);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(hash);
    }

    private static void ValidateRequired(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }
    }

    private static void ValidateOptional(
        string? value,
        string parameterName,
        int maxLength)
    {
        if (value?.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
