using FluentValidation;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Auth.Register;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(256)
            .Must(UserAggregate.IsValidEmail).WithMessage("Email address is not in a valid format.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.StudentId)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.StudentId));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.MajorOrClass)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.MajorOrClass));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Address));
    }
}
