using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.ResendVerificationCode;

public record ResendVerificationCodeCommand(string Email) : ICommand<ResendVerificationCodeResponse>;
