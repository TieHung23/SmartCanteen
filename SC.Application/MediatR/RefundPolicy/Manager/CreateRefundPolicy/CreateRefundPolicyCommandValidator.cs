using FluentValidation;

namespace SC.Application.MediatR.RefundPolicy.Manager.CreateRefundPolicy;

public sealed class CreateRefundPolicyCommandValidator
    : AbstractValidator<CreateRefundPolicyCommand>
{
    public CreateRefundPolicyCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9_.-]+$");

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.Description)
            .MaximumLength(500);

        RuleFor(command => command.Percent)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}
