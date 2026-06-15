using FluentValidation;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Auth.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(256)
            .Must(UserAggregate.IsValidEmail).WithMessage("Email address is not in a valid format.");
    }
}
