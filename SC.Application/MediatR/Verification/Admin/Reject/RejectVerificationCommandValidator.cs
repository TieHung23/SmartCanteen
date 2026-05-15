using FluentValidation;

namespace SC.Application.MediatR.Verification.Admin.Reject;

public class RejectVerificationCommandValidator : AbstractValidator<RejectVerificationCommand>
{
    public RejectVerificationCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10).WithMessage("Rejection reason must be at least 10 characters.")
            .MaximumLength(500);
    }
}
