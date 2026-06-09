using FluentValidation;

namespace SC.Application.MediatR.Refund.Manager.RejectRefundRequest;

public sealed class RejectRefundRequestCommandValidator
    : AbstractValidator<RejectRefundRequestCommand>
{
    public RejectRefundRequestCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
