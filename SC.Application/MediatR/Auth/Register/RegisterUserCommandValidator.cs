using System.Text.RegularExpressions;
using FluentValidation;
using SC.Domain.Domain.User.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Auth.Register;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly Regex VietnamesePhoneRegex = new(
        @"^(0|\+84)(3|5|7|8|9)\d{8}$",
        RegexOptions.Compiled);

    private static readonly Regex PasswordSpecialCharRegex = new(
        @"[!@#$%^&*()_\-+=\[\]{}|;:'""<>,.?/~`]",
        RegexOptions.Compiled);

    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(256)
            .Must(UserAggregate.IsValidEmail).WithMessage("Email address is not in a valid format.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Must(p => p != null && PasswordSpecialCharRegex.IsMatch(p))
                .WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.Gender!.Value)
            .IsInEnum()
            .When(x => x.Gender.HasValue);

        RuleFor(x => x.DateOfBirth!.Value)
            .LessThan(_ => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddYears(-10))
                .WithMessage("Date of birth must indicate the user is at least 10 years old.")
            .GreaterThan(_ => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddYears(-100))
                .WithMessage("Date of birth must be within the last 100 years.")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("StudentId is required for Student category.")
            .When(x => x.Category == UserCategory.Student);

        RuleFor(x => x.StudentId)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.StudentId));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Must(p => VietnamesePhoneRegex.IsMatch(p!))
                .WithMessage("Phone number must be a valid Vietnamese number (e.g. 0912345678 or +84912345678).")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.MajorOrClass)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.MajorOrClass));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Address));
    }
}
