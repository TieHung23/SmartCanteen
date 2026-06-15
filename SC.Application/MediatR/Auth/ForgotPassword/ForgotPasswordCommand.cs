using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommand<PasswordActionResponse>;
