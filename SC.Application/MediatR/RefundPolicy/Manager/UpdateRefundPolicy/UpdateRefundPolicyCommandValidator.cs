using FluentValidation;

namespace SC.Application.MediatR.RefundPolicy.Manager.UpdateRefundPolicy;

public sealed class UpdateRefundPolicyCommandValidator
    : AbstractValidator<UpdateRefundPolicyCommand>
{
    public UpdateRefundPolicyCommandValidator()
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
