using FluentValidation;
using SC.Application.MediatR.Auth.Shared;

namespace SC.Application.MediatR.Auth.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy();
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Password confirmation does not match.");
    }
}
