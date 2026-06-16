using FluentValidation;
using SC.Domain.SharedKernel;

namespace SC.Application.MediatR.Notification.UnregisterDeviceToken;

public sealed class UnregisterDeviceTokenCommandValidator
    : AbstractValidator<UnregisterDeviceTokenCommand>
{
    public UnregisterDeviceTokenCommandValidator()
    {
        RuleFor(command => command)
            .Must(command =>
                !string.IsNullOrWhiteSpace(command.Token)
                || !string.IsNullOrWhiteSpace(command.DeviceId))
            .WithMessage("Token or deviceId is required.");

        RuleFor(command => command.Token)
            .MaximumLength(DeviceTokenConstraints.TokenMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Token));

        RuleFor(command => command.DeviceId)
            .MaximumLength(DeviceTokenConstraints.DeviceIdMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.DeviceId));
    }
}
