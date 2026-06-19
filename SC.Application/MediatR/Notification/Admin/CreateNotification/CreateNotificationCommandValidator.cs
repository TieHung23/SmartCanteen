using FluentValidation;
using SC.Domain.SharedKernel;

namespace SC.Application.MediatR.Notification.Admin.CreateNotification;

public sealed class CreateNotificationCommandValidator
    : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(command => command.RecipientId)
            .NotEmpty();

        RuleFor(command => command.Type)
            .NotEmpty()
            .MaximumLength(NotificationConstraints.TypeMaxLength)
            .Matches("^[A-Za-z0-9_.-]+$");

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(NotificationConstraints.TitleMaxLength);

        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(NotificationConstraints.MessageMaxLength);

        RuleFor(command => command.ReferenceType)
            .MaximumLength(NotificationConstraints.ReferenceTypeMaxLength)
            .Matches("^[A-Za-z0-9_.-]+$")
            .When(command => !string.IsNullOrWhiteSpace(command.ReferenceType));

        RuleFor(command => command.ReferenceType)
            .NotEmpty()
            .When(command => command.ReferenceId.HasValue);

        RuleFor(command => command.ActionUrl)
            .MaximumLength(NotificationConstraints.ActionUrlMaxLength)
            .Must(BeSafeRelativeUrl)
            .WithMessage("ActionUrl must be a local relative path beginning with a single '/'.")
            .When(command => !string.IsNullOrWhiteSpace(command.ActionUrl));
    }

    private static bool BeSafeRelativeUrl(string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl))
        {
            return true;
        }

        var value = actionUrl.Trim();
        return value.StartsWith('/')
               && !value.StartsWith("//", StringComparison.Ordinal)
               && !value.Any(char.IsControl);
    }
}
