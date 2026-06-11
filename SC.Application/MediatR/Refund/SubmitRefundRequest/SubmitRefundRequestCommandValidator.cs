using FluentValidation;

namespace SC.Application.MediatR.Refund.SubmitRefundRequest;

public sealed class SubmitRefundRequestCommandValidator
    : AbstractValidator<SubmitRefundRequestCommand>
{
    public SubmitRefundRequestCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.PolicyCode)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.Images)
            .Must(images => images.Count <= 5)
            .WithMessage("A refund request can contain at most 5 images.");
    }
}
