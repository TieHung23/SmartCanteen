using System.Text.RegularExpressions;
using FluentValidation;

namespace SC.Application.MediatR.Auth.Shared;

internal static class PasswordRules
{
    private static readonly Regex SpecialCharacterRegex = new(
        @"[!@#$%^&*()_\-+=\[\]{}|;:'""<>,.?/~`]",
        RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Must(SpecialCharacterRegex.IsMatch)
                .WithMessage("Password must contain at least one special character.");
    }
}
