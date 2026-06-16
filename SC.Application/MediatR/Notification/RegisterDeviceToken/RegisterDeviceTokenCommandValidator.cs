using FluentValidation;
using SC.Contract.Services.Notification;
using SC.Domain.SharedKernel;

namespace SC.Application.MediatR.Notification.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandValidator
    : AbstractValidator<RegisterDeviceTokenCommand>
{
    private static readonly string[] AllowedPlatforms =
    [
        DeviceTokenPlatforms.Android,
        DeviceTokenPlatforms.Ios
    ];

    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty()
            .MaximumLength(DeviceTokenConstraints.TokenMaxLength);

        RuleFor(command => command.Platform)
            .NotEmpty()
            .MaximumLength(DeviceTokenConstraints.PlatformMaxLength)
            .Must(platform =>
                !string.IsNullOrWhiteSpace(platform)
                && AllowedPlatforms.Contains(
                    platform.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            .WithMessage("Platform must be Android or iOS.");

        RuleFor(command => command.DeviceId)
            .MaximumLength(DeviceTokenConstraints.DeviceIdMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.DeviceId));

        RuleFor(command => command.AppVersion)
            .MaximumLength(DeviceTokenConstraints.AppVersionMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.AppVersion));
    }
}
