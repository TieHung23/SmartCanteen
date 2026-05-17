using FluentValidation;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Auth.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(256)
            .Must(UserAggregate.IsValidEmail).WithMessage("Email address is not in a valid format.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(100);
    }
}
