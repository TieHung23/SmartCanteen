using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.ResetPassword;

public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword) : ICommand<PasswordActionResponse>;
